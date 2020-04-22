Imports System.Net

Namespace oData

    ''' <summary>
    ''' An inherited Exception for storing oData/http errors.
    ''' </summary>
    Public Class oException : Inherits Exception

        Private _StatusCode As HttpStatusCode = HttpStatusCode.Unused
        ''' <summary>
        ''' The HttpStatusCode of the message.
        ''' Returns HttpStatusCode.Unused if no code returned.
        ''' </summary>
        ''' <returns>HttpStatusCode</returns>
        Public ReadOnly Property StatusCode As HttpStatusCode
            Get
                Return _StatusCode
            End Get
        End Property

#Region "Ctor"

        ''' <summary>
        ''' Constructor Method.
        ''' </summary>
        ''' <param name="Message">String</param>
        Sub New(StatusCode As HttpStatusCode, Message As String)
            MyBase.New(Message)
            _StatusCode = StatusCode

        End Sub

#End Region

    End Class

End Namespace

