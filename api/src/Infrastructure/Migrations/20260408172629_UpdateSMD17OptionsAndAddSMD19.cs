using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class UpdateSMD17OptionsAndAddSMD19 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Update existing SMD17 answer values to use new option keys
        // Old values were: 0=All Family Hubs sites, 1=Family Hub Network site, 2=Hospital,
        //                  3=Early years setting, 4=Home visits, 5=Virtual, 6=Other locations in LA, 7=Other
        // New values are: all_hub_sites, some_hub_sites, all_network_sites, some_network_sites,
        //                 home_visits, early_years_setting, primary_school, hospital, virtual, other

        // Map old numeric values to new descriptive keys
        // Note: The mapping is based on the closest semantic match
        // 0 (All Family Hubs sites) -> all_hub_sites
        // 1 (Family Hub Network site) -> all_network_sites (was network site, now all network sites)
        // 2 (Hospital) -> hospital
        // 3 (Early years setting) -> early_years_setting
        // 4 (Home visits) -> home_visits
        // 5 (Virtual) -> virtual
        // 6 (Other locations in LA) -> other
        // 7 (Other) -> other

        migrationBuilder.Sql(@"
                UPDATE service_answers
                SET value = REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(
                                            REPLACE(value, '7', 'other'),
                                        '6', 'other'),
                                    '5', 'virtual'),
                                '4', 'home_visits'),
                            '3', 'early_years_setting'),
                        '2', 'hospital'),
                    '1', 'all_network_sites'),
                '0', 'all_hub_sites')
                WHERE question_code = 'SMD17'
                AND value ~ '^[0-7](,[0-7])*$';
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert SMD17 answer values back to numeric keys
        migrationBuilder.Sql(@"
                UPDATE service_answers
                SET value = REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(
                                            REPLACE(value, 'other', '7'),
                                        'virtual', '5'),
                                    'home_visits', '4'),
                                'early_years_setting', '3'),
                            'hospital', '2'),
                        'all_network_sites', '1'),
                    'all_hub_sites', '0'),
                'some_hub_sites', '1')
                WHERE question_code = 'SMD17';
            ");
    }
}