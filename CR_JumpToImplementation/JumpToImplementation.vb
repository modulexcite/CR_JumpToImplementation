Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms
Imports DevExpress.CodeRush.Core
Imports DevExpress.CodeRush.PlugInCore
Imports DevExpress.CodeRush.StructuralParser
Imports DevExpress.CodeRush.Library

Public Class JumpToImplementation
#Region " InitializePlugIn "
    Public Overrides Sub InitializePlugIn()
        MyBase.InitializePlugIn()
        Call CreateNavigationItem()
    End Sub

#End Region
#Region " FinalizePlugIn "
    Public Overrides Sub FinalizePlugIn()
        Call DestroyNavigationItem()
        MyBase.FinalizePlugIn()
    End Sub
#End Region

    Public NavProvider As NavigationProvider
    Private Sub CreateNavigationItem()
        NavProvider = New NavigationProvider()
        Dim initialize As ISupportInitialize = CType(NavProvider, ISupportInitialize)
        initialize.BeginInit()
        Try
            NavProvider.ProviderName = "First Implementation"
            NavProvider.Description = "First Implementation"
            NavProvider.Register = True
            AddHandler NavProvider.Apply, AddressOf ImplementorFromInterfaceDeclaration_Apply
            AddHandler NavProvider.CheckAvailability, AddressOf ImplementorFromInterfaceDeclaration_CheckAvailability
        Finally
            initialize.EndInit()
        End Try
    End Sub
    Private Sub DestroyNavigationItem()
        If NavProvider Is Nothing Then
            Return
        End If
        RemoveHandler NavProvider.Apply, AddressOf ImplementorFromInterfaceDeclaration_Apply
        RemoveHandler NavProvider.CheckAvailability, AddressOf ImplementorFromInterfaceDeclaration_CheckAvailability
        NavProvider.Dispose()
        NavProvider = Nothing
    End Sub

#Region "Main Functionality"
    Private Sub JumpToImplementation(ByVal ActiveElement As LanguageElement)
        If TypeOf ActiveElement Is Member Then
            Call JumpToInterfaceMemberImplementation(ActiveElement)
        ElseIf TypeOf ActiveElement Is [Interface] Then
            Call JumpToInterfaceImplementation(ActiveElement)
        End If
    End Sub

    Private Sub JumpToInterfaceMemberImplementation(ByVal ActiveElement As LanguageElement)
        ' Attempt Interface member first
        Dim Member As Member = CType(ActiveElement, Member)
        If Member IsNot Nothing Then
            If TypeOf Member.Parent Is [Interface] Then
                Dim TheInterface As [Interface] = CType(Member.Parent, [Interface])
                Dim TheClass As [Class] = GetFirstClassImplementingInterface(TheInterface)
                Dim TheMember As Member = GetInterfaceMemberImplementor(TheClass, Member)
                Call JumpToLangageElement(TheMember)
            End If
        End If
    End Sub

    Private Sub JumpToInterfaceImplementation(ByVal ActiveElement As LanguageElement)
        ' Use whole interface if nessecary
        Dim TheClass As [Class] = GetFirstClassImplementingInterface(CType(ActiveElement, [Interface]))
        If TheClass IsNot Nothing Then
            ' Locate Class
            Call JumpToLangageElement(TheClass)
        End If
    End Sub
    Private Sub JumpToLangageElement(ByVal FoundElement As LanguageElement)
        If FoundElement Is Nothing Then
            Exit Sub
        End If
        Call FoundElement.Document.Activate()
        Call CodeRush.Documents.ActiveTextView.Selection.Set(FoundElement.NameRange)
    End Sub
#End Region

#Region "Implementor Location Functions"
    Private Function GetFirstClassImplementingInterface(ByVal TheInterface As [Interface]) As [Class]
        Dim Implementations As ITypeElement() = TheInterface.GetDescendants()
        Dim Found As Boolean = False
        For Each Imp In Implementations
            Dim TheDeclaration As IElement = Imp.GetDeclaration
            If TheDeclaration IsNot Nothing Then
                Return CType(TheDeclaration, [Class])
            End If
        Next
        Return Nothing
    End Function
    Private Function GetInterfaceMemberImplementor(ByVal TheClass As [Class], ByVal InterfaceMember As Member) As Member
        ' find the Member on the whose signature matches
        For Each Node As LanguageElement In TheClass.Nodes
            If TypeOf Node Is Member Then
                Dim ClassMember As Member = CType(Node, Member)
                If ImplementsInterface(ClassMember, InterfaceMember) Then
                    Return ClassMember
                End If
            End If
        Next
        Return Nothing
    End Function
    Function ImplementsInterface(ByVal Member As IMemberElement, ByVal InterfaceMember As IMemberElement) As Boolean
        If Member Is Nothing OrElse InterfaceMember Is Nothing Then
            Return False
        End If
        Dim interfaceType As IInterfaceElement = If(TypeOf InterfaceMember.ParentType Is IInterfaceElement, CType(InterfaceMember.ParentType, IInterfaceElement), Nothing)
        If interfaceType Is Nothing Then
            Return False
        End If
        Dim memberType As ITypeElement = If(TypeOf Member.ParentType Is ITypeElement, CType(Member.ParentType, ITypeElement), Nothing)
        If memberType Is Nothing Then
            Return False
        End If
        If Not memberType.DescendsFrom(interfaceType) Then
            Return False
        End If
        Dim resovler As ISourceTreeResolver = New SourceTreeResolver()
        Dim MemberWithParams As IWithParameters = If(TypeOf Member Is IWithParameters, CType(Member, IWithParameters), Nothing)
        Dim InterfaceMemberWithParams As IWithParameters = If(TypeOf InterfaceMember Is IWithParameters, CType(InterfaceMember, IWithParameters), Nothing)
        If Not SignatureHelper.SignaturesMatch(resovler, MemberWithParams, InterfaceMemberWithParams) Then
            Return False
        End If
        Return True
    End Function

#End Region

#Region "Action and NavigationProvider Events"
    Private Sub actJumpToImplementation_Execute(ByVal ea As DevExpress.CodeRush.Core.ExecuteEventArgs) Handles actJumpToImplementation.Execute
        Call JumpToImplementation(CodeRush.Source.Active)
    End Sub
#Region "ImplementorFromInterfaceDeclaration"
    Private Sub ImplementorFromInterfaceDeclaration_Apply(ByVal sender As Object, ByVal ea As DevExpress.CodeRush.Core.ApplyContentEventArgs)
        Call JumpToImplementation(ea.Element)
    End Sub
    Private Sub ImplementorFromInterfaceDeclaration_CheckAvailability(ByVal sender As Object, ByVal ea As DevExpress.CodeRush.Core.CheckContentAvailabilityEventArgs)
        ea.Available = TypeOf ea.Element Is [Interface] OrElse TypeOf ea.Element.Parent Is [Interface]
    End Sub
#End Region
    Private Sub ImplementorFromObjectWhoseClassImplementsInterface_Navigate(ByVal sender As Object, ByVal ea As DevExpress.CodeRush.Core.ApplyContentEventArgs)
        Call JumpToImplementation(ea.Element)
    End Sub
    Private Sub ImplementorFromObjectWhoseClassImplementsInterface_CheckAvailability(ByVal sender As Object, ByVal ea As DevExpress.CodeRush.Core.CheckContentAvailabilityEventArgs)
        Dim StartElement As LanguageElement = ea.Element
        If Not StartElement.ElementType = LanguageElementType.MethodCall Then
            Exit Sub
        End If
        Dim MethodCall As MethodCall = CType(StartElement, MethodCall)
        'If MethodCall.Then Then

        'End If
        'If Start Then

        'End If
        ea.Available = TypeOf ea.Element Is [Interface] _
        OrElse TypeOf ea.Element.Parent Is [Interface]
    End Sub
#End Region

End Class
