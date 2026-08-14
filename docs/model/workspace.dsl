workspace extends https://raw.githubusercontent.com/wgt-system/architecture/dev/model/workspace.dsl {
    model {
        !element wgt {
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
        properties {
            "structurizr.sort" "created"
        }

        container wgt WgtContainers {
            include windowsClient
            include iphoneClient
            include windowsStore
            include iphoneStore
            autolayout lr 220 120
            title "Wiiii Got This — Containers"
            description "Accepted WGT client-host and device-local persistence boundaries for Windows and iPhone."
        }

        styles {
            element "Application Host" {
                shape RoundedBox
                width 380
                height 220
                background #1F5A67
                color #FFFFFF
                stroke #263238
                strokeWidth 2
                fontSize 20
                description false
            }

            element "Local Store" {
                shape Cylinder
                width 320
                height 190
                background #5A526F
                color #FFFFFF
                stroke #263238
                strokeWidth 2
                fontSize 19
                description false
            }

            relationship "Relationship" {
                color #667085
                routing Direct
                thickness 2
                width 240
                fontSize 16
            }
        }
    }

    configuration {
        scope softwaresystem
    }
}
