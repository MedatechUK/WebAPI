Imports System.Web

Namespace Web

    Public Interface xmlFeed
        'Sub ProcessRequest(ByVal context As HttpContext)
        'Sub SetMeta(ByRef Metadata As xmlFeedProps)
        'Sub Install(ByVal context As HttpContext)

    End Interface

    Public Interface xmlFeedProps
        ReadOnly Property EndPoint As String
        ReadOnly Property Hidden As Boolean

    End Interface

End Namespace
