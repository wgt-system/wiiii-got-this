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

    relationship "WGT Internal" {
        color #667085
        routing Direct
        thickness 2
        width 240
        fontSize 16
    }
}
