using System;
using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 1, "1.6.2" )]
    class InstallSystemData : ExtendedMigration
    {
        public override void Up()
        {
            //
            // Create the content channel type.
            //
            Sql( @"
INSERT INTO [ContentChannelType]
	([IsSystem], [Name], [DateRangeType], [Guid], [DisablePriority], [IncludeTime], [DisableContentField])
	VALUES (1, 'Digital Signage', 2, 'D5F479C6-2460-4021-8BB0-40F652494783', 1, 1, 1)
" );
            var contentChannelTypeId = ( int ) SqlScalar( "SELECT [Id] FROM [ContentChannelType] WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.ContentChannelType.DIGITAL_SIGNAGE } } );

            //
            // Add the attributes to the content channel.
            //
            RockMigrationHelper.AddEntityAttribute( "Rock.Model.ContentChannelItem", Rock.SystemGuid.FieldType.IMAGE,
                "ContentChannelTypeId", contentChannelTypeId.ToString(), "Slide", string.Empty,
                "The image to be displayed on screen.", 0, string.Empty,
                SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_SLIDE, "com_shepherdchurch_Slide" );

            RockMigrationHelper.AddEntityAttribute( "Rock.Model.ContentChannelItem", Rock.SystemGuid.FieldType.URL_LINK,
                "ContentChannelTypeId", contentChannelTypeId.ToString(), "Video", string.Empty,
                "The image to be displayed on screen.", 0, string.Empty,
                SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_VIDEO_URL, "com_shepherdchurch_Video" );

            //
            // Add the digital sign schedules defined type.
            //
            RockMigrationHelper.AddDefinedType( "Global", "Digital Sign Content Schedules",
                "Defines what content is displayed on digital sign kiosks with the specified scheduling options. Leave schedule blank to show at all times. The first ordered match from the top down will be the content channel that is displayed.",
                SystemGuid.DefinedType.DIGITAL_SIGN_CONTENT_SCHEDULES );
            RockMigrationHelper.AddDefinedTypeAttribute( SystemGuid.DefinedType.DIGITAL_SIGN_CONTENT_SCHEDULES,
                Rock.SystemGuid.FieldType.SCHEDULES, "Schedules", "com_shepherdchurch_Schedules",
                "Select all the schedules you want this content channel to be active for. If no schedules are selected then this schedule always matches.",
                0, string.Empty, SystemGuid.Attribute.DEFINED_VALUE_DIGITAL_SIGN_CONTENT_SCHEDULES_SCHEDULES );
            RockMigrationHelper.AddDefinedTypeAttribute( SystemGuid.DefinedType.DIGITAL_SIGN_CONTENT_SCHEDULES,
                Rock.SystemGuid.FieldType.CONTENT_CHANNEL, "Content Channel", "com_shepherdchurch_ContentChannel",
                "The content channel that will be displayed for a matched schedule.",
                1, string.Empty, SystemGuid.Attribute.DEFINED_VALUE_DIGITAL_SIGN_CONTENT_SCHEDULES_CONTENT_CHANNEL );

            //
            // Add the device type.
            //
            RockMigrationHelper.AddDefinedValue( Rock.SystemGuid.DefinedType.DEVICE_TYPE,
                "Digital Display", "A computer or device acting as a digital sign display",
                SystemGuid.DefinedValue.DEVICE_TYPE_DIGITAL_DISPLAY, true );
            var deviceTypeValueId = ( int ) SqlScalar( "SELECT [Id] FROM [DefinedValue] WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.DefinedValue.DEVICE_TYPE_DIGITAL_DISPLAY } } );

            //
            // Add the device type attribute.
            //
            RockMigrationHelper.AddEntityAttribute( "Rock.Model.Device", Rock.SystemGuid.FieldType.MULTI_SELECT,
                "DeviceTypeValueId", deviceTypeValueId.ToString(), "Content Schedules", string.Empty,
                "Available content schedules to display on this device. The first active content channel will be used.", 0, string.Empty,
                SystemGuid.Attribute.DEVICE_CONTENT_SCHEDULES, "com_shepherdchurch_ContentSchedules" );
            RockMigrationHelper.AddAttributeQualifier( SystemGuid.Attribute.DEVICE_CONTENT_SCHEDULES,
                "values", "SELECT [DV].[Value] AS [Text], [DV].[Guid] AS [Value] FROM [DefinedValue] AS [DV] INNER JOIN [DefinedType] AS [DT] ON [DT].[Id] = [DV].[DefinedTypeId] WHERE [DT].[Guid] = ''6958000D-6CB1-4077-825A-4214513CC8FB'' ORDER BY [DV].[Order]",
                SystemGuid.AttributeQualifier.DEVICE_CONTENT_SCHEDULES_VALUES );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.DEVICE_CONTENT_SCHEDULES );
            RockMigrationHelper.DeleteDefinedValue( SystemGuid.DefinedValue.DEVICE_TYPE_DIGITAL_DISPLAY );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.DEFINED_VALUE_DIGITAL_SIGN_CONTENT_SCHEDULES_CONTENT_CHANNEL );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.DEFINED_VALUE_DIGITAL_SIGN_CONTENT_SCHEDULES_SCHEDULES );
            RockMigrationHelper.DeleteDefinedType( SystemGuid.DefinedType.DIGITAL_SIGN_CONTENT_SCHEDULES );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_VIDEO_URL );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_SLIDE );
            
            Sql( "DELETE FROM [ContentChannelType] WHERE [Guid] = 'D5F479C6-2460-4021-8BB0-40F652494783'" );
        }
    }
}
