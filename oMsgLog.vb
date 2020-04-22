Namespace Web

#Region "Enumerations"

    Public Enum LogEntryType As Integer
        Err = 1
        Information = 4
        FailureAudit = 16
        SuccessAudit = 8
        Warning = 2
    End Enum

    Public Enum EvtLogVerbosity As Integer
        Normal = 1
        Verbose = 10
        VeryVerbose = 50
        Arcane = 99
    End Enum

    Public Enum EvtLogSource As Integer
        APPLICATION
        SYSTEM
        WEB
    End Enum

#End Region

    Public Class oMsgLog

#Region "Constructors"

        Public Sub New()

        End Sub

        Public Sub New(
            ByVal svcType As String,
               Optional ByVal LogSource As EvtLogSource = EvtLogSource.APPLICATION,
               Optional ByVal Verbosity As EvtLogVerbosity = EvtLogVerbosity.VeryVerbose,
               Optional ByVal EntryType As LogEntryType = LogEntryType.Information
            )

            _svcType = svcType
            _LogSource = LogSource
            _Verbosity = Verbosity
            _EntryType = EntryType
            _EventOnly = 0

        End Sub

        Public Sub New(ex As Exception)
            _Verbosity = EvtLogVerbosity.Normal
            _EntryType = LogEntryType.FailureAudit
            _LogSource = EvtLogSource.SYSTEM
            _LogData.Append(ex.Message).AppendLine()
            _LogData.Append(ex.StackTrace).AppendLine()
        End Sub

        Public Sub setException(ex As Exception)
            _Verbosity = EvtLogVerbosity.Normal
            _EntryType = LogEntryType.FailureAudit
            _LogData.Append(ex.Message).AppendLine()
            _LogData.Append(ex.StackTrace).AppendLine()
        End Sub

#End Region

#Region "Message Properies"

        Private _svcType As String
        Public Property svcType() As String
            Get
                Return _svcType
            End Get
            Set(ByVal value As String)
                _svcType = value
            End Set
        End Property

        Private _LogSource As EvtLogSource
        Public Property LogSource() As EvtLogSource
            Get
                Return _LogSource
            End Get
            Set(ByVal value As EvtLogSource)
                _LogSource = value
            End Set
        End Property

        Private _Verbosity As EvtLogVerbosity
        Public Property Verbosity() As EvtLogVerbosity
            Get
                Return _Verbosity
            End Get
            Set(ByVal value As EvtLogVerbosity)
                _Verbosity = value
            End Set
        End Property

        Private _EntryType As LogEntryType
        Public Property EntryType() As LogEntryType
            Get
                Return _EntryType
            End Get
            Set(ByVal value As LogEntryType)
                _EntryType = value
            End Set
        End Property

        Private _LogData As New System.Text.StringBuilder
        Public Property LogData() As System.Text.StringBuilder
            Get
                Return _LogData
            End Get
            Set(ByVal value As System.Text.StringBuilder)
                _LogData = value
            End Set
        End Property

        Private _EventOnly As Integer = 0
        Public Property EventOnly() As Integer
            Get
                Return _EventOnly
            End Get
            Set(ByVal value As Integer)
                _EventOnly = value
            End Set
        End Property

#End Region

    End Class

End Namespace