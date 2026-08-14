!element wgt {
    properties {
        "structurizr.inspection.model.softwaresystem.documentation" "info"
        "structurizr.inspection.model.softwaresystem.decisions" "info"
    }

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

    windowsClient -> windowsStore "Stores and retrieves WGT-owned local state" "Microsoft.Data.Sqlite / SQLite" {
        tags "WGT Internal"
    }
    iphoneClient -> iphoneStore "Stores and retrieves WGT-owned local state" "Microsoft.Data.Sqlite / SQLite" {
        tags "WGT Internal"
    }
}
