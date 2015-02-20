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

using System.Diagnostics;

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
            LogHelper.Info<SimpilyForumCacheHandler>("Updated Cache for {0} [{1}] found {2} items in {2}ms",
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