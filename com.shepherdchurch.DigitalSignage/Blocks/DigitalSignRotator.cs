using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using com.shepherdchurch.DigitalSignage.ViewModels;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Model;
using Rock.Utility.ExtensionMethods;
using Rock.Web.Cache;

namespace com.shepherdchurch.DigitalSignage.Blocks;

[DisplayName( "Digital Sign Rotator" )]
[Category( "Shepherd Church > Digital Signage" )]
[Description( "Displays a full-screen interface for displaying images and videos in a rotator." )]

#region Block Attributes

[IntegerField( "Slide Interval",
    Description = "How long each slide should remain on screen before the next transition happens. Default is 8 seconds. Must be at least 4 seconds. If a value is set on the Content Channel then that value will be used instead.",
    IsRequired = false,
    DefaultIntegerValue = 0,
    Key = AttributeKeys.SlideInterval,
    Order = 0 )]

[IntegerField( "Update Interval",
    Description = "How often should the slide rotator check for updates to the content channel. Default is 60 seconds. Must be at least 10 seconds.",
    IsRequired = false,
    DefaultIntegerValue = 0,
    Key = AttributeKeys.UpdateInterval,
    Order = 1 )]

[CustomCheckboxListField( "Transitions",
    Description = "Which transitions should be used. If none are selected then all available transitions will be used. If a value is set on the Content Channel then that value will be used instead.",
    ListSource = "bars^Bars,blinds^Blinds,blocks^Blocks,blocks2^Blocks 2,dissolve^Dissolve,slide^Slide,zip^Zip,bars3d^Bars 3D,blinds3d^Blinds 3D,cube^Cube 3D,tiles3d^Tiles 3D,turn^Turn 3D",
    IsRequired = false,
    Key = AttributeKeys.Transitions,
    Order = 2 )]

[ContentChannelField( "Content Channel Override",
    Description = "By default the configured schedules on the device will determine what content channel to display. You can override this behavior to always show this content channel no matter what.",
    IsRequired = false,
    Key = AttributeKeys.ContentChannelOverride,
    Order = 3 )]

[BooleanField( "Enable Device Match By Name",
    Description = "Enable a match by computer name by doing reverse IP lookup to get computer name based on IP address",
    DefaultBooleanValue = true,
    Key = AttributeKeys.EnableReverseLookup,
    Order = 4 )]

#endregion

[Rock.SystemGuid.EntityTypeGuid( "31fac716-2c84-461c-a4d1-63f77845da91" )]
[Rock.SystemGuid.BlockTypeGuid( "096ec773-8e15-42b5-b247-dbef215d148c" )]
[ConfigurationChangedReload( Rock.Enums.Cms.BlockReloadMode.Block )]
public class DigitalSignRotator : RockBlockType
{
    #region Keys

    public static class AttributeKeys
    {
        public const string SlideInterval = "SlideInterval";

        public const string UpdateInterval = "UpdateInterval";

        public const string Transitions = "Transitions";

        public const string ContentChannelOverride = "ContentChannelOverride";

        public const string EnableReverseLookup = "EnableReverseLookup";
    }

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override string ObsidianFileUrl => "~/Plugins/com_shepherdchurch/DigitalSignage/digitalSignRotator.obs";

    #endregion

    #region Methods

    /// <inheritdoc/>
    public override object GetObsidianBlockInitialization()
    {
        var deviceService = new DeviceService( RockContext );
        var digitalDisplayId = DefinedValueCache.Get( SystemGuid.DefinedValue.DEVICE_TYPE_DIGITAL_DISPLAY.AsGuid(), RockContext ).Id;
        var ipAddress = RequestContext.ClientInformation.IpAddress;
        var enableReverseLookup = GetAttributeValue( AttributeKeys.EnableReverseLookup ).AsBoolean( true );
        Device device = null;
        ContentChannelCache contentChannel = null;

        if ( !string.IsNullOrWhiteSpace( PageParameter( "deviceId" ) ) )
        {
            device = deviceService.Get( PageParameter( "deviceId" ).AsInteger() );
        }
        else
        {
            device = deviceService.Queryable()
                .Where( d => d.DeviceTypeValueId == digitalDisplayId && d.IPAddress == ipAddress )
                .FirstOrDefault();

            if ( device == null && enableReverseLookup )
            {
                try
                {
                    var hostName = System.Net.Dns.GetHostEntry( ipAddress ).HostName;
                    device = deviceService.Queryable()
                        .Where( d => d.DeviceTypeValueId == digitalDisplayId && d.IPAddress == hostName )
                        .FirstOrDefault();
                }
                catch
                {
                    /* Intentionally ignored. */
                }
            }
        }

        if ( Guid.TryParse( GetAttributeValue( AttributeKeys.ContentChannelOverride ), out var contentChannelGuid ) )
        {
            contentChannel = ContentChannelCache.Get( contentChannelGuid, RockContext );
        }

        RequestContext.Response.AddCssLink( RequestContext.ResolveRockUrl( "~/Plugins/com_shepherdchurch/DigitalSignage/Styles/digitalsignrotator.css" ), true );
        RequestContext.Response.AddScriptLinkToHead( RequestContext.ResolveRockUrl( "~/Plugins/com_shepherdchurch/DigitalSignage/Scripts/flux.min.js" ), true );
        RequestContext.Response.AddScriptLinkToHead( RequestContext.ResolveRockUrl( "~/Plugins/com_shepherdchurch/DigitalSignage/Scripts/digitalsignrotator.js" ), true );

        // If we don't have the required information then show an error.
        if ( device == null && contentChannel == null )
        {
            return new DigitalSignRotatorConfigurationBag
            {
                ErrorMessage = $"This kiosk has not been configured in the system (IP address: {ipAddress}).",
            };
        }

        var bag = new DigitalSignRotatorConfigurationBag
        {
            DeviceId = device?.Id,
            ContentChannelId = contentChannel?.Id,
            IsAudioEnabled = PageParameter( "Audio" ).AsBoolean( true ),
            Transitions = GetAttributeValue( AttributeKeys.Transitions )?.SplitDelimitedValues( false ).ToList(),
        };

        if ( GetAttributeValue( AttributeKeys.SlideInterval ).AsInteger() >= 4 )
        {
            bag.SlideInterval = GetAttributeValue( AttributeKeys.SlideInterval ).AsInteger() * 1000;
        }

        if ( GetAttributeValue( AttributeKeys.UpdateInterval ).AsInteger() >= 10 )
        {
            bag.UpdateInterval = GetAttributeValue( AttributeKeys.UpdateInterval ).AsInteger() * 1000;
        }

        return bag;
    }

    /// <summary>
    /// Get all the content channel items that should be displayed.
    /// </summary>
    /// <param name="contentChannel">The content channel whose items are to be displayed.</param>
    /// <returns>A SignContents object that contains the formatted data for the client.</returns>
    private SignContents GetContentChannelItems( List<int> campusIds, ContentChannel contentChannel, List<int> visitedContentChannels = null )
    {
        var contents = new SignContents();
        var items = contentChannel.Items
            .Where( i => i.StartDateTime <= RockDateTime.Now )
            .Where( i => i.ExpireDateTime == null || i.ExpireDateTime > RockDateTime.Now )
            .Where( i => !contentChannel.RequiresApproval || i.Status == ContentChannelItemStatus.Approved );

        // Add ourselves to the visited content channels so we don't recurse forever.
        visitedContentChannels ??= [];
        visitedContentChannels.Add( contentChannel.Id );

        // Get any configuration options from the content channel.
        contentChannel.LoadAttributes( RockContext );
        if ( contentChannel.GetAttributeValue( "com_shepherdchurch_SlideInterval" ).AsInteger() >= 4 )
        {
            contents.SlideInterval = contentChannel.GetAttributeValue( "com_shepherdchurch_SlideInterval" ).AsInteger() * 1000;
        }
        if ( !string.IsNullOrWhiteSpace( contentChannel.GetAttributeValue( "com_shepherdchurch_Transitions" ) ) )
        {
            contents.Transitions = [.. contentChannel.GetAttributeValues( "com_shepherdchurch_Transitions" )];
        }

        // Order the items either manually or by start date time, depending on configuration.
        if ( contentChannel.ItemsManuallyOrdered )
        {
            items = items.OrderBy( i => i.Order );
        }
        else
        {
            items = items.OrderBy( i => i.StartDateTime );
        }

        // Loop each content item and see if it contains something to display.
        foreach ( var item in items )
        {
            if ( item.Attributes == null )
            {
                item.LoadAttributes( RockContext );
            }

            // Check for valid campus.
            var campusFilter = item.GetAttributeValues( "com_shepherdchurch_CampusFilter" )
                .AsGuidOrNullList()
                .Where( g => g.HasValue )
                .Select( g => CampusCache.Get( g.Value ) )
                .Where( c => c != null )
                .Select( c => c.Id )
                .ToList();

            if ( campusFilter.Count > 0 && campusIds.Count > 0 && campusFilter.Intersect( campusIds ).Count() == 0 )
            {
                continue;
            }

            var duration = item.GetAttributeValue( "com_shepherdchurch_Duration" ).AsIntegerOrNull();

            // Check which kind of slide to include.
            if ( !string.IsNullOrWhiteSpace( item.GetAttributeValue( "com_shepherdchurch_IncludeContentFrom" ) ) )
            {
                // Process the "Include Content From" attribute.
                var guid = item.GetAttributeValue( "com_shepherdchurch_IncludeContentFrom" ).AsGuid();
                var childContentChannel = new ContentChannelService( RockContext ).Get( guid );

                if ( childContentChannel != null && !visitedContentChannels.Contains( childContentChannel.Id ) )
                {
                    var childContents = GetContentChannelItems( campusIds, childContentChannel, visitedContentChannels );

                    contents.Audio.AddRange( childContents.Audio );
                    contents.Slides.AddRange( childContents.Slides );
                }
            }
            else if ( !string.IsNullOrWhiteSpace( item.GetAttributeValue( "com_shepherdchurch_SlideUrl" ) ) )
            {
                // Process the "Slide Url" attribute.
                var fileUrl = item.GetAttributeValue( "com_shepherdchurch_SlideUrl" );
                if ( Regex.IsMatch( fileUrl, "\\.mp3(\\?|$)", RegexOptions.IgnoreCase ) )
                {
                    contents.Audio.Add( fileUrl );
                }
                else
                {
                    contents.Slides.Add( new Slide( fileUrl, duration ) );
                }
            }
            else
            {
                // Process the "Slide" attribute.
                // Check if it contains either an audio file or an image.
                var guid = item.GetAttributeValue( "com_shepherdchurch_Slide" ).AsGuid();
                var binaryFile = new BinaryFileService( RockContext ).Get( guid );

                if ( binaryFile != null && binaryFile.Id != 0 )
                {
                    if ( binaryFile.MimeType.StartsWith( "audio/" ) )
                    {
                        contents.Audio.Add( RequestContext.ResolveRockUrl( string.Format( "~/GetFile.ashx?guid={0}", binaryFile.Guid ) ) );
                    }
                    else if ( binaryFile.MimeType.StartsWith( "image/" ) )
                    {
                        contents.Slides.Add( new Slide( RequestContext.ResolveRockUrl( string.Format( "~/GetImage.ashx?guid={0}", binaryFile.Guid ) ) , duration ) );
                    }
                }
            }
        }

        return contents;
    }

    #endregion

    #region Block Actions

    [BlockAction]
    public BlockActionResult GetDeviceFeed( int deviceId )
    {
        var deviceService = new DeviceService( RockContext );
        var scheduleService = new ScheduleService( RockContext );
        var contentChannelService = new ContentChannelService( RockContext );
        var response = new SlidesResponse();

        Device device = deviceService.Get( deviceId );

        if ( device != null )
        {
            var campuses = device.Locations.Select( l => l.CampusId ).Where( c => c.HasValue && c.Value != 0 ).Select( c => c.Value ).ToList();

            device.LoadAttributes( RockContext );
            var definedValueGuids = device.GetAttributeValue( "com_shepherdchurch_ContentSchedules" ).SplitDelimitedValues().AsGuidList();
            var definedValues = new List<DefinedValueCache>();

            // Build a list of the cached defined values so we can then sort by Order.
            foreach ( var definedValueGuid in definedValueGuids )
            {
                var definedValue = DefinedValueCache.Get( definedValueGuid );

                if ( definedValue != null )
                {
                    definedValues.Add( definedValue );
                }
            }

            // Check each defined value they have selected on this device and look for the
            // first one that is active.
            foreach ( var definedValue in definedValues.OrderBy( d => d.Order ) )
            {
                var contentChannel = contentChannelService.Get( definedValue.GetAttributeValue( "com_shepherdchurch_ContentChannel" ).AsGuid() );

                if ( contentChannel != null )
                {
                    var schedules = definedValue.GetAttributeValues( "com_shepherdchurch_Schedules" ).AsGuidList();
                    bool scheduleActive = false;

                    // Check if either no schedules (match by default) or any single schedule
                    // is currently active.
                    if ( !schedules.Any() )
                    {
                        scheduleActive = true;
                    }
                    else
                    {
                        foreach ( var guid in schedules )
                        {
                            var schedule = scheduleService.Get( guid );

                            if ( schedule.WasScheduleActive( RockDateTime.Now ) )
                            {
                                scheduleActive = true;
                                break;
                            }
                        }
                    }

                    // If the schedule is active, then this is the content channel we are going
                    // to work with. Build our list of image URLs and audio URLs.
                    if ( scheduleActive )
                    {
                        response.Contents = GetContentChannelItems( campuses, contentChannel );

                        break;
                    }
                }
            }

            response.GenerateHash();
        }

        return ActionOk( response );
    }

    [BlockAction]
    public BlockActionResult GetContentChannelFeed( int contentChannelId )
    {
        var contentChannelService = new ContentChannelService( RockContext );
        var response = new SlidesResponse();

        var contentChannel = contentChannelService.Get( contentChannelId );

        if ( contentChannel != null )
        {
            response.Contents = GetContentChannelItems( new List<int>(), contentChannel );
        }

        response.GenerateHash();

        return ActionOk( response );
    }

    #endregion

    #region Utility Classes

    public class Slide
    {
        public string Url { get; set; }

        public int? Duration { get; set; }

        public Slide()
        {
        }

        public Slide( string url, int? duration )
        {
            Url = url;
            Duration = duration;
        }
    }

    /// <summary>
    /// This is a helper class for returning the API data to the client.
    /// </summary>
    public class SignContents
    {
        public List<string> Audio { get; set; }

        public List<Slide> Slides { get; set; }

        public int SlideInterval { get; set; }

        public string[] Transitions { get; set; }

        public SignContents()
        {
            Audio = new List<string>();
            Slides = new List<Slide>();
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
