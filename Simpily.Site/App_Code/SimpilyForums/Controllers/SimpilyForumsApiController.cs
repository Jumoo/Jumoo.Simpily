using Umbraco.Web.WebApi;
using Umbraco.Core.Logging;

namespace Jumoo.Simpily
{

    /// <summary>
    /// Summary description for SimplyForumsApiController
    /// </summary>
    public class SimpilyForumsApiController : UmbracoApiController
    {
        /// <summary>
        /// used by the front end to delete posts via ajax.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool DeletePost(int id)
        {
            var _contentService = ApplicationContext.Services.ContentService;
            var post = _contentService.GetById(id);

            if (post != null)
            {
                var author = post.GetValue<int>("postAuthor");

                if (author > 0 && author == Members.GetCurrentMemberId())
                {
                    LogHelper.Info<SimpilyForumsApiController>("Deleting post {0}", () => id);
                    if ( post.HasProperty("umbracoNaviHide")) 
                        post.SetValue("umbracoNaviHide", true);

                    if ( post.HasProperty("deletedByAuthor"))
                        post.SetValue("deletedByAuthor", true);

                    _contentService.SaveAndPublishWithStatus(post);
                    LogHelper.Info<SimpilyForumsApiController>("Deleting post {0}", () => id);
                    return true;
                }
            }
            return false;
        }
    }
}