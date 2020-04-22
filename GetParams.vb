Imports System.Web

Namespace Web

    Public Class GetParams : Inherits Dictionary(Of String, String)

        Public Sub New()
            With HttpContext.Current.Request
                If .Url.ToString.IndexOf("?") > 0 Then
                    For Each pair As String In .Url.ToString.Substring(.Url.ToString.IndexOf("?") + 1).Split("&")
                        If pair.IndexOf("=") > 0 Then
                            Dim k As String = pair.Split("=")(0)
                            Dim v As String = pair.Split("=")(1)
                            If Me.Keys.Contains(k) Then
                                Me(k) = String.Format("{0},{1}", Me(k), v)
                            Else
                                Add(k, v)
                            End If

                        Else
                            If Not Me.Keys.Contains(pair) Then
                                Add(pair, "")
                            End If

                        End If
                    Next
                End If

            End With
        End Sub

    End Class

End Namespace