Imports Newtonsoft.Json

Namespace oData

    Public Class JTextWriter : Inherits JsonTextWriter

        Public Sub New(textwriter As IO.TextWriter)
            MyBase.New(textwriter)

        End Sub

        Public Sub WriteAttributeString(name As String, value As String)
            With Me
                '.WriteStartObject()
                .WritePropertyName(name)
                .WriteValue(value)
                '.WriteEndObject()
            End With

        End Sub

        Public Sub WriteAttributeObject(name As String, value As Object)
            With Me
                '.WriteStartObject()
                .WritePropertyName(name)
                .WriteValue(value)
                '.WriteEndObject()
            End With

        End Sub

    End Class

End Namespace