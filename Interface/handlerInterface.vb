Imports System.Web

Namespace Web

    Public Interface xmlHandler
        'Sub ProcessRequest(ByRef context As HttpContext)
        'Sub SetMeta(ByRef Metadata As xmlHandlerProps)

    End Interface

    Public Interface xmlHandlerProps
        ReadOnly Property EndPoint As String
        ReadOnly Property Hidden As Boolean

    End Interface

End Namespace
