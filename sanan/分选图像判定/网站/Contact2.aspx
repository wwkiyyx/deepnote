<%@ Page Title="图档" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contact2.aspx.cs" Inherits="WebApplication3.Contact2" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    
    <div class="row">
        <div class="col-md-2">
            <asp:Calendar ID="Calendar1" runat="server" OnSelectionChanged="Calendar1_SelectionChanged"></asp:Calendar>
            <asp:Label ID="Label1" runat="server" Text="0"></asp:Label>
            <br>
            <asp:Button ID="Button4" runat="server" Text="原图1" OnClick="Button4_Click" />
            <asp:Button ID="Button5" runat="server" Text="原图3" OnClick="Button5_Click" />
            <br>
            <asp:Label ID="Label2" runat="server" Text="机台:"></asp:Label>
            <asp:DropDownList ID="DropDownList1" runat="server" AutoPostBack="True" OnSelectedIndexChanged="DropDownList1_SelectedIndexChanged"></asp:DropDownList>
        </div>
        <div class="col-md-3">
            <asp:GridView ID="GridView1" runat="server" AutoGenerateSelectButton="True" OnSelectedIndexChanged="GridView1_SelectedIndexChanged" AllowPaging="True" OnPageIndexChanging="GridView1_PageIndexChanging" PageSize="20">
                <SelectedRowStyle BackColor="#333333" ForeColor="White" />
            </asp:GridView>
        </div>
        <div class="col-md-1">
            <asp:GridView ID="GridView2" runat="server" AutoGenerateSelectButton="True" OnSelectedIndexChanged="GridView2_SelectedIndexChanged" AllowPaging="True" OnPageIndexChanging="GridView2_PageIndexChanging" PageSize="20">
                <SelectedRowStyle BackColor="#333333" ForeColor="White" />
            </asp:GridView>
        </div>
        <div class="col-md-6">
            <asp:Image ID="Image1" runat="server" />
            <br>
            <asp:Chart ID="Chart1" runat="server">
                <Series>
                    <asp:Series Name="Series1"></asp:Series>
                </Series>
                <ChartAreas>
                    <asp:ChartArea Name="ChartArea1"></asp:ChartArea>
                </ChartAreas>
            </asp:Chart>
        </div>
    </div>
    <div class="row">
        <div class="col-md-12">
            <asp:Image ID="Image2" runat="server" Height="100%" Width="100%" />
        </div>
    </div>

</asp:Content>
