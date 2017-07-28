using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Web.Cache;

namespace com.shepherdchurch.DigitalSignage.Rest
{
    public class DigitalSignRotatorController : ApiController, IHasCustomRoutes
    {
        /// <summary>
        /// Add in the routes that are supported by this plugin.
        /// </summary>
        /// <param name="routes">The RouteCollection that we should add any additional routes to.</param>
        public void AddRoutes( System.Web.Routing.RouteCollection routes )
        {
            routes.MapHttpRoute(
                name: "ShepherdChurchDigitalSignageSlides",
                routeTemplate: "api/com.shepherdchurch/DigitalSignage/{type}/{id}",
                defaults: new
                {
                    controller = "DigitalSignRotator"
                } );
        }

        #region API Methods

        /// <summary>
        /// Retrieve the content that should currently be displayed on the device.
        /// </summary>
        /// <param name="id">The device identifier whose content should be retrieved.</param>
        /// <returns>An object that contains the content and the content hash for change detection.</returns>
        [System.Web.Http.Route( "api/com.shepherdchurch/DigitalSignage/Device/{id}" )]
        public SlidesResponse GetSlides( string id )
        {
            var rockContext = new RockContext();
            var deviceService = new DeviceService( rockContext );
            var scheduleService = new ScheduleService( rockContext );
            var contentChannelService = new ContentChannelService( rockContext );
            var response = new SlidesResponse();

            Device device = deviceService.Get( id.AsInteger() );

            if ( device != null )
            {
                device.LoadAttributes( rockContext );
                var definedValueGuids = device.GetAttributeValue( "com_shepherdchurch_ContentSchedules" ).SplitDelimitedValues().AsGuidList();
                List<DefinedValueCache> definedValues = new List<DefinedValueCache>();

                //
                // Build a list of the cached defined values so we can then sort by Order.
                //
                foreach ( var definedValueGuid in definedValueGuids )
                {
                    var definedValue = DefinedValueCache.Read( definedValueGuid );

                    if ( definedValue != null )
                    {
                        definedValues.Add( definedValue );
                    }
                }

                //
                // Check each defined value they have selected on this device and look for the
                // first one that is active.
                //
                foreach ( var definedValue in definedValues.OrderBy( d => d.Order ) )
                {
                    var contentChannel = contentChannelService.Get( definedValue.GetAttributeValue( "com_shepherdchurch_ContentChannel" ).AsGuid() );

                    if ( contentChannel != null )
                    {
                        var schedules = definedValue.GetAttributeValues( "com_shepherdchurch_Schedules" ).AsGuidList();
                        bool scheduleActive = false;

                        //
                        // Check if either no schedules (match by default) or any single schedule
                        // is currently active.
                        //
                        if ( !schedules.Any() )
                        {
                            scheduleActive = true;
                        }
                        else
                        {
                            foreach ( var guid in schedules )
                            {
                                var schedule = scheduleService.Get( guid );

                                if ( schedule.IsScheduleActive )
                                {
                                    scheduleActive = true;
                                    break;
                                }
                            }
                        }

                        //
                        // If the schedule is active, then this is the content channel we are going
                        // to work with. Build our list of image URLs and audio URLs.
                        //
                        if ( scheduleActive )
                        {
                            response.Contents = GetContentChannelItems( contentChannel, rockContext );

                            break;
                        }
                    }
                }

                response.GenerateHash();
            }

            return response;
        }

        /// <summary>
        /// Get the content to be displayed for a specific content channel.
        /// </summary>
        /// <param name="id">The ContentChannel identifier whose content will be displayed.</param>
        /// <returns>An object that contains the content and the content hash for change detection.</returns>
        [System.Web.Http.Route( "api/com.shepherdchurch/DigitalSignage/ContentChannel/{id}" )]
        public SlidesResponse GetContentChannel( string id )
        {
            var rockContext = new RockContext();
            var contentChannelService = new ContentChannelService( rockContext );
            var response = new SlidesResponse();

            var contentChannel = contentChannelService.Get( id.AsInteger() );

            if ( contentChannel != null )
            {
                response.Contents = GetContentChannelItems( contentChannel, rockContext );
            }

            response.GenerateHash();

            return response;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Get all the content channel items that should be displayed.
        /// </summary>
        /// <param name="contentChannel">The content channel whose items are to be displayed.</param>
        /// <param name="rockContext">The context to load any extra data from.</param>
        /// <returns>A SignContents object that contains the formatted data for the client.</returns>
        private SignContents GetContentChannelItems( ContentChannel contentChannel, RockContext rockContext )
        {
            var contents = new SignContents();
            var items = contentChannel.Items.Where( i => i.StartDateTime <= DateTime.Now && ( i.ExpireDateTime == null || i.ExpireDateTime > DateTime.Now ) );

            //
            // Order the items either manually or by start date time, depending on configuration.
            //
            if ( contentChannel.ItemsManuallyOrdered )
            {
                items = items.OrderBy( i => i.Order );
            }
            else
            {
                items = items.OrderBy( i => i.StartDateTime );
            }

            //
            // Loop each content item and see if it contains something to display.
            //
            foreach ( var item in items )
            {
                Guid guid = Guid.Empty;
                BinaryFile binaryFile = null;
                string fileUrl;

                item.LoadAttributes( rockContext );

                //
                // Now check for a video file.
                //
                fileUrl = item.GetAttributeValue( "com_shepherdchurch_Video" );
                if ( !string.IsNullOrWhiteSpace( fileUrl ) )
                {
                    if ( Regex.IsMatch( fileUrl, "\\.mp3(\\?|$)", RegexOptions.IgnoreCase ) )
                    {
                        contents.Audio.Add( fileUrl );
                    }
                    else
                    {
                        contents.Slides.Add( fileUrl );
                    }
                }
                else
                {
                    //
                    // Check if it contains either an audio file or an image.
                    //
                    guid = item.GetAttributeValue( "com_shepherdchurch_Slide" ).AsGuid();
                    binaryFile = new BinaryFileService( rockContext ).Get( guid );

                    if ( binaryFile != null && binaryFile.Id != 0 )
                    {
                        if ( binaryFile.MimeType.StartsWith( "audio/" ) )
                        {
                            contents.Audio.Add( VirtualPathUtility.ToAbsolute( string.Format( "~/GetFile.ashx?id={0}", binaryFile.Id ) ) );
                        }
                        else if ( binaryFile.MimeType.StartsWith( "image/" ) )
                        {
                            contents.Slides.Add( VirtualPathUtility.ToAbsolute( string.Format( "~/GetImage.ashx?id={0}", binaryFile.Id ) ) );
                        }
                    }
                }
            }

            return contents;
        }

        #endregion

        #region Utility Classes

        /// <summary>
        /// This is a helper class for returning the API data to the client.
        /// </summary>
        public class SignContents
        {
            public List<string> Audio { get; set; }

            public List<string> Slides { get; set; }

            public SignContents()
            {
                Audio = new List<string>();
                Slides = new List<string>();
            }
        }

        /// <summary>
        /// This is a helper class for returning the API data to the client.
        /// </summary>
        public class SlidesResponse
        {
            public string Hash { get; set; }

            public SignContents Contents { get; set; }

            public SlidesResponse()
            {
                Hash = string.Empty;
                Contents = new SignContents();
            }

            /// <summary>
            /// Calculate and store the hash of the content. This is used by clients to
            /// determine if the contents have changed from the last time they requested
            /// an update.
            /// </summary>
            public void GenerateHash()
            {
                var sha1 = SHA1CryptoServiceProvider.Create();

                Hash = Convert.ToBase64String( sha1.ComputeHash( Encoding.ASCII.GetBytes( Contents.ToJson() ) ) );
            }
        }

        #endregion
    }
}
