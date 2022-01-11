<%@ Page Title="图表" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="WebApplication3.Contact" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    
    <div class="row">
        <div class="col-md-2">
            <asp:Calendar ID="Calendar1" runat="server" OnSelectionChanged="Calendar1_SelectionChanged"></asp:Calendar>
            <asp:Label ID="Label1" runat="server" Text="0"></asp:Label>
            <br>
            <asp:Button ID="Button4" runat="server" Text="YOLO" OnClick="Button4_Click" />
            <asp:Button ID="Button5" runat="server" Text="原图" OnClick="Button5_Click" />
            <br>
            <asp:CheckBox ID="CheckBox1" runat="server" Checked="True" Text="OK" />
            <asp:CheckBox ID="CheckBox2" runat="server" Checked="True" Text="NG" />
            <asp:CheckBox ID="CheckBox3" runat="server" Checked="True" Text="复判" />
            <br>
            <asp:Label ID="Label2" runat="server" Text="机台:"></asp:Label>
            <asp:DropDownList ID="DropDownList1" runat="server" AutoPostBack="True" OnSelectedIndexChanged="DropDownList1_SelectedIndexChanged"></asp:DropDownList>
        </div>
        <div class="col-md-3">
            <%-- <asp:Label ID="Label4" runat="server" Text="当前选择编号:"></asp:Label> --%>
            <%-- <asp:Label ID="Label2" runat="server" Text="X"></asp:Label> --%>
            <asp:GridView ID="GridView1" runat="server" AutoGenerateSelectButton="True" OnSelectedIndexChanged="GridView1_SelectedIndexChanged" AllowPaging="True" OnPageIndexChanging="GridView1_PageIndexChanging" PageSize="20">
                <SelectedRowStyle BackColor="#333333" ForeColor="White" />
            </asp:GridView>
        </div>
        <div class="col-md-1">
            <%-- <asp:Label ID="Label5" runat="server" Text="当前选择编号:"></asp:Label> --%>
            <%-- <asp:Label ID="Label3" runat="server" Text="X"></asp:Label> --%>
            <asp:GridView ID="GridView2" runat="server" AutoGenerateSelectButton="True" OnSelectedIndexChanged="GridView2_SelectedIndexChanged" AllowPaging="True" OnPageIndexChanging="GridView2_PageIndexChanging" PageSize="20">
                <SelectedRowStyle BackColor="#333333" ForeColor="White" />
            </asp:GridView>
        </div>
        <div class="col-md-6">
            <%-- <asp:Button ID="Button1" runat="server" Text="人工判定OK" OnClick="Button1_Click" /> --%>
            <%-- <asp:Button ID="Button2" runat="server" Text="人工判定NG" OnClick="Button2_Click" /> --%>
            <%-- <asp:Button ID="Button3" runat="server" Text="人工判定图像出错(无效图像)" OnClick="Button3_Click" /> --%>
            <asp:Image ID="Image1" runat="server" />
        </div>
    </div>
    <div class="row">
        <div class="col-md-12">
            <asp:Image ID="Image2" runat="server" />
        </div>
    </div>

</asp:Content>
