workspace extends https://raw.githubusercontent.com/wgt-system/architecture/dev/model/workspace.dsl {
    model {
        !include model.dsl
    }

    views {
        properties {
            "structurizr.sort" "created"
        }

        !include views.dsl
    }

    configuration {
        scope softwaresystem
    }
}
