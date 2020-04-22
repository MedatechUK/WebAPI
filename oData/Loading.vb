
Namespace oData

    Public Class Loading : Implements IDisposable

        Private o As ODAT_TRANS
        Private otrans As rowODAT_TRANS

        Private _StrType As String
        Private bubbleid As String

        ''' <summary>
        ''' oData Loading Constructor
        ''' </summary>
        ''' <param name="strtype">Transaction Type</param>
        Sub New(strtype As String)

            bubbleid = System.Guid.NewGuid.ToString
            _StrType = strtype

            o = New ODAT_TRANS(Reflection.Assembly.GetExecutingAssembly)
            otrans = o.AddRow()
            With otrans
                .TYPENAME = _StrType
                .BUBBLEID = bubbleid

            End With

        End Sub

        ''' <summary>
        ''' Add a row to the loading.
        ''' </summary>
        ''' <param name="Recordtype">Integer record type for load row</param>
        ''' <returns>the created row</returns>
        Public Function AddRow(Recordtype As Integer) As rowODAT_LOAD

            Dim ret As rowODAT_LOAD = otrans.ODAT_LOAD.AddRow
            With ret
                .RECORDTYPE = Recordtype.ToString
            End With

            Return ret

        End Function

        ''' <summary>
        ''' Post the data to the server specified in the odata.config
        ''' </summary>
        ''' <returns>List(of exception)</returns>
        Public Function Post() As List(Of Exception)

            Dim ex As New List(Of Exception)

            o.Post()
            ErCheck(o, ex)
            If ex.Count = 0 Then
                Using F As New ODAT_TRANS(Reflection.Assembly.GetExecutingAssembly)
                    With F
                        With .AddRow()
                            .TYPENAME = _StrType
                            .BUBBLEID = bubbleid
                            .COMPLETE = "Y"

                        End With

                        .Post()
                        ErCheck(F, ex)

                    End With

                End Using

            End If

            Return ex

        End Function

        ''' <summary>
        ''' Recursively check the result for errors
        ''' </summary>
        ''' <param name="f">oForm to Check</param>
        ''' <param name="EX">List of exceptions</param>
        Private Sub ErCheck(f As oForm, ByRef EX As List(Of Exception))
            For Each row As oRow In f
                If Not row.Exception Is Nothing Then
                    EX.Add(row.Exception)

                End If
                For Each sf As oForm In row.SubForms.Values
                    ErCheck(sf, EX)

                Next
            Next

        End Sub

#Region "IDisposable Support"

        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).

                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class

End Namespace