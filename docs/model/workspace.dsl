workspace {
    name "Wiiii Got This — Architecture"
    description "Service-owned design-time architecture model for Wiiii Got This. It visualizes the accepted WGT runtime topology and remains subordinate to WGT documentation and ADRs; system-wide ownership remains governed by wgt-system/architecture."
    !impliedRelationships false

    model {
        wgt = softwareSystem "Wiiii Got This" "Cross-platform integration and presentation bounded context for independently owned service capabilities." {
            tags "Bounded Context"
            url "https://github.com/wgt-system/wiiii-got-this"

            windowsClient = container "Windows Client" "Primary Wiiii Got This application host on Windows, containing WGT composition, domain/application behavior, presentation, infrastructure, and statically shipped Integration Adapters." ".NET 10 / Avalonia 12 / Windows" {
                tags "Application Host"
            }

            iphoneClient = container "iPhone Client" "Primary Wiiii Got This application host on iPhone, containing WGT composition, domain/application behavior, presentation, infrastructure, and statically shipped Integration Adapters." ".NET 10 / Avalonia 12 / iOS" {
                tags "Application Host"
            }

            windowsStore = container "Windows Local Store" "Device-local SQLite store for WGT-owned integration, device, configuration, and validated publication metadata on Windows." "SQLite" {
                tags "Local Store"
            }

            iphoneStore = container "iPhone Local Store" "Device-local SQLite store for WGT-owned integration, device, configuration, and validated publication metadata on iPhone." "SQLite" {
                tags "Local Store"
            }

            windowsClient -> windowsStore "Stores and retrieves WGT-owned local state" "Microsoft.Data.Sqlite / SQLite"
            iphoneClient -> iphoneStore "Stores and retrieves WGT-owned local state" "Microsoft.Data.Sqlite / SQLite"
        }
    }

    views {
        container wgt WgtContainers {
            include wgt
            include windowsClient
            include iphoneClient
            include windowsStore
            include iphoneStore
            autolayout lr
            title "Wiiii Got This — Containers"
            description "Accepted WGT client-host and device-local persistence boundaries for Windows and iPhone."
        }

        terminology {
            softwareSystem "Bounded Context"
        }

        styles {
            element "Bounded Context" {
                shape RoundedBox
                background #F7F8FA
                color #263238
            }

            element "Application Host" {
                shape RoundedBox
                background #1F5A67
                color #FFFFFF
                fontSize 24
            }

            element "Local Store" {
                shape Cylinder
                background #5A526F
                color #FFFFFF
                fontSize 22
            }

            relationship "Relationship" {
                color #667085
                thickness 2
                fontSize 18
            }
        }
    }

    configuration {
        scope softwaresystem
    }
}
