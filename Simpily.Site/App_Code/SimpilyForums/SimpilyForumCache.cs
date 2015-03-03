﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Umbraco.Core.Services;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;

using umbraco.interfaces;

using Umbraco.Core.Logging;
using Umbraco.Core;

using Umbraco.Web;
using Umbraco.Web.Models;
using System.Web.Caching;

using Examine;
using Examine.SearchCriteria;
using Examine.LuceneEngine.SearchCriteria;

using System.Diagnostics;
using System.Text;
using System.Globalization;

namespace Jumoo.Simpily
{
    /// <summary>
    /// Caches things like last post date, and post count, so the site doesn't
    /// have to work it out all the time...
    /// 
    /// we attache to the publish / unpublish events, so we can clear the cache
    /// when a new post arrives or someone does something with the back end.
    /// </summary>
    public class SimpilyForumCacheHandler : ApplicationEventHandler
    {
        private int postContentTypeId;
        private int forumContentTypeId;

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            var postType = ApplicationContext.Current.Services.ContentTypeService.GetContentType("Simpilypost");
            if (postType != null)
                postContentTypeId = postType.Id;

            var forumType = ApplicationContext.Current.Services.ContentTypeService.GetContentType("Simpilyforum");
            if (forumType != null)
                forumContentTypeId = forumType.Id;

            ContentService.Published += ContentServicePublished;
            ContentService.UnPublished += ContentServicePublished;
        }

        void ContentServicePublished(Umbraco.Core.Publishing.IPublishingStrategy sender, Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            // when something is published, (if it's a ForumPost)
            // clear the relevant forum cache.
            // we do it in two steps because more than one post in a forum 
            // may have been published, so we only need to clear the cache
            // once.

            List<string> invalidCacheList = new List<string>();

            foreach (var item in e.PublishedEntities)
            {
                // is a forum post...
                if (item.ContentTypeId == postContentTypeId)
                {
                    // get parent Forum.
                    invalidCacheList = AddParentForumCaches(item, invalidCacheList);
                }
            }

            // clear the cache for any forums that have had child pages published...
            foreach (var cache in invalidCacheList)
            {
                LogHelper.Info<SimpilyForumCacheHandler>("Clearing Forum Info Cache: {0}", () => cache);
                ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheByKeySearch(cache);
            }
            
        }

        private List<string> AddParentForumCaches(IContent item, List<string> cacheList)
        {

            var parent = item.Parent();

            if (parent != null)
            {
                if (parent.ContentTypeId == forumContentTypeId || parent.ContentTypeId == postContentTypeId)
                {
                    var cache = string.Format("simpilyforum_{0}", parent.Id);
                    if (!cacheList.Contains(cache))
                        cacheList.Add(cache);

                    cacheList = AddParentForumCaches(parent, cacheList);
                }
            }

            return cacheList;
        }
    }

    public static class SimpliyForumCache 
    {
        /// <summary>
        ///  Get the Forum Info - using examine, because that can be faster when 
        ///  there are lots and lots of posts - although i've yet to see it 
        ///  really get faster than the traverse method (yet)
        /// </summary>
        /// <param name="useExamine">true to use examine</param>
        public static SimpilyForumCacheItem GetForumInfo(this IPublishedContent item, bool useExamine)
        {
            if (useExamine == false)
                return GetForumInfo(item);

            var cacheName = string.Format("simpilyforum_{0}", item.Id);
            var cache = UmbracoContext.Current.Application.ApplicationCache;
            var forumInfo = cache.GetCacheItem<SimpilyForumCacheItem>(cacheName);

            if (forumInfo != null)
                return forumInfo;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            forumInfo = new SimpilyForumCacheItem();
             

            // examine cache - because that's faster ? 
            var searcher = ExamineManager.Instance.SearchProviderCollection["InternalSearcher"];
            var searchCriteria = searcher.CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);

            var query = new StringBuilder();
            query.AppendFormat("-{0}:1 ", "umbracoNaviHide");
            query.AppendFormat("+__NodeTypeAlias:simpilypost +path:\\-1*{0}*", item.Id);

            var filter = searchCriteria.RawQuery(query.ToString());
            var results = searcher.Search(filter).OrderByDescending(x => x.Fields["updateDate"]);

            forumInfo.Count = results.ToList().Count;
            forumInfo.latestPost = DateTime.MinValue;

            if ( results.Any() )
            {
                var update = DateTime.ParseExact(results.First().Fields["updateDate"], "yyyyMMddHHmmssfff", CultureInfo.CurrentCulture);
                if (update > DateTime.MinValue)
                   forumInfo.latestPost = update; 

                foreach(var field in results.First().Fields)
                {
                    LogHelper.Info<SimpilyForumCacheHandler>("Field: {0} {1}", () => field.Key, ()=> field.Value);
                }
            }
            cache.InsertCacheItem<SimpilyForumCacheItem>(cacheName, CacheItemPriority.Default, () => forumInfo);

            sw.Stop();
            LogHelper.Info<SimpilyForumCacheHandler>("Updated Cache (using Examine) for {0} [{1}] found {2} items in {3}ms",
                () => item.Name, () => item.Id, () => forumInfo.Count, () => sw.ElapsedMilliseconds);

            return forumInfo;
        }

        public static SimpilyForumCacheItem GetForumInfo(this IPublishedContent item)
        {
            var cacheName = string.Format("simpilyforum_{0}", item.Id);
            var cache = UmbracoContext.Current.Application.ApplicationCache;
            var forumInfo = cache.GetCacheItem<SimpilyForumCacheItem>(cacheName);

            if (forumInfo != null)
                return forumInfo;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // not in the cache, we have to make it.
            forumInfo = new SimpilyForumCacheItem();

            var posts = item.DescendantsOrSelf().Where(x => x.IsVisible() && x.DocumentTypeAlias == "Simpilypost");

            forumInfo.Count = posts.Count();
            if (posts.Any())
            {
                var lastPost = posts.OrderByDescending(x => x.UpdateDate).FirstOrDefault();
                forumInfo.latestPost = lastPost.UpdateDate;
            }

            cache.InsertCacheItem<SimpilyForumCacheItem>(cacheName, CacheItemPriority.Default, () => forumInfo);

            sw.Stop();
            LogHelper.Info<SimpilyForumCacheHandler>("Updated Cache for {0} [{1}] found {2} items in {3}ms",
                () => item.Name, () => item.Id, ()=> forumInfo.Count, ()=> sw.ElapsedMilliseconds);

            return forumInfo;
        }
    }

    public class SimpilyForumCacheItem : IComparable
    {
        public int Count { get; set; }
        public DateTime latestPost { get; set; }

        public int CompareTo(object obj)
        {
            return latestPost.CompareTo(((SimpilyForumCacheItem)obj).latestPost);
        }
    }
}