using System;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations;

[MigrationNumber( 11, "17.6" )]
public class ConvertToObsidian : ExtendedMigration
{
    public override void Up()
    {
        RockMigrationHelper.AddOrUpdateEntityType(
            "com.shepherdchurch.DigitalSignage.Blocks.DigitalSignRotator",
            "31fac716-2c84-461c-a4d1-63f77845da91",
            false,
            false );
        
        ConvertWebFormsBlockToObsidian( new Guid( SystemGuid.BlockType.DIGITAL_SIGN_ROTATOR ),
            new Guid( "31fac716-2c84-461c-a4d1-63f77845da91" ) );
    }

    public override void Down()
    {
        ConvertObsidianBlockToWebForms( new Guid( SystemGuid.BlockType.DIGITAL_SIGN_ROTATOR ),
            "~/Plugins/com_shepherdchurch/DigitalSignage/Digital Sign Rotator.ascx" );
    }
}
