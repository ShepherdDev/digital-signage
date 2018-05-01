using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 8, "1.6.4" )]
    public class AddCampusFilter : ExtendedMigration
    {
        public override void Up()
        {
            var contentChannelTypeId = ( int ) SqlScalar( "SELECT [Id] FROM [ContentChannelType] WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.ContentChannelType.DIGITAL_SIGNAGE } } );

            //
            // Add the attributes to the content channel.
            //
            RockMigrationHelper.AddEntityAttribute( "Rock.Model.ContentChannelItem", Rock.SystemGuid.FieldType.SINGLE_SELECT,
                "ContentChannelTypeId", contentChannelTypeId.ToString(), "Include Content From", string.Empty,
                "Includes the content from the specified content channel, takes precedence over Slide and Slide Url fields.",
                2, string.Empty,
                SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_INCLUDE_CONTENT_FROM, "com_shepherdchurch_IncludeContentFrom" );

            RockMigrationHelper.AddAttributeQualifier( SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_INCLUDE_CONTENT_FROM,
                "values",
                string.Format( "SELECT [Guid] AS [Value], [Name] AS [Text] FROM [ContentChannel] WHERE [ContentChannelTypeId] = {0}", contentChannelTypeId ),
                SystemGuid.AttributeQualifier.CONTENT_CHANNEL_ITEM_INCLUDE_CONTENT_FROM_VALUES );

            RockMigrationHelper.AddEntityAttribute( "Rock.Model.ContentChannelItem", Rock.SystemGuid.FieldType.CAMPUSES,
                "ContentChannelTypeId", contentChannelTypeId.ToString(), "Campus Filter", string.Empty,
                "Which campuses this slide will be displayed at. If nothing is selected then all campuses are active.",
                3, string.Empty,
                SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_CAMPUS_FILTER, "com_shepherdchurch_CampusFilter" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_CAMPUS_FILTER );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_INCLUDE_CONTENT_FROM );
        }
    }
}
