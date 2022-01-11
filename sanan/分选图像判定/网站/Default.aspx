<%@ Page Title="查询" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebApplication3._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="row">
        <div class="col-md-2">
            <asp:Label ID="Label1" runat="server" Text="日期:"></asp:Label>
            <asp:DropDownList ID="DropDownList1" runat="server" AutoPostBack="True" OnSelectedIndexChanged="DropDownList1_SelectedIndexChanged"></asp:DropDownList>
        </div>
        <div class="col-md-3">
            <asp:Label ID="Label2" runat="server" Text="批号:"></asp:Label>
            <asp:DropDownList ID="DropDownList2" runat="server" AutoPostBack="True" OnSelectedIndexChanged="DropDownList2_SelectedIndexChanged"></asp:DropDownList>
        </div>
        <div class="col-md-7">
            <asp:Label ID="Label3" runat="server" Text="图片:"></asp:Label>
            <asp:DropDownList ID="DropDownList3" runat="server" AutoPostBack="True" OnSelectedIndexChanged="DropDownList3_SelectedIndexChanged"></asp:DropDownList>
        </div>
    </div>
    <div class="row">
        <div class="col-md-6">
            <asp:Image ID="Image1" runat="server" />
        </div>
        <div class="col-md-6">
            <asp:Image ID="Image2" runat="server" />
        </div>
    </div>
    <div class="row">
        <div class="col-md-12">
            <asp:Image ID="Image3" runat="server" />
        </div>
    </div>
    <div class="jumbotron">
        <h1>使用说明：</h1>
        <p class="lead">1、选择日期；</p>
        <p class="lead">2、选择批号；</p>
        <p class="lead">3、选择图片。</p>
    </div>
    <div class="row">
        <div class="col-md-2">
            <asp:Calendar ID="Calendar1" runat="server" OnSelectionChanged="Calendar1_SelectionChanged" SelectionMode="DayWeek"></asp:Calendar>
        </div>
        <div class="col-md-10">
            <asp:Label ID="Label4" runat="server" Text="机台:"></asp:Label>
            <asp:DropDownList ID="DropDownList4" runat="server" AutoPostBack="True" OnSelectedIndexChanged="DropDownList4_SelectedIndexChanged"></asp:DropDownList>
            <asp:HyperLink ID="HyperLink1" runat="server">导出IP</asp:HyperLink>
        </div>
    </div>
    <div class="row">
        <div class="col-md-6">
            <asp:Chart ID="Chart1" runat="server" Height="6000px" Width="500px">
                <Series>
                    <asp:Series Name="Series1" ChartType="StackedBar100" Legend="Legend1" LegendText="OK"></asp:Series>
                    <asp:Series ChartArea="ChartArea1" ChartType="StackedBar100" Legend="Legend1" LegendText="RE" Name="Series2">
                    </asp:Series>
                    <asp:Series ChartArea="ChartArea1" ChartType="StackedBar100" Legend="Legend1" LegendText="NG" Name="Series3">
                    </asp:Series>
                </Series>
                <ChartAreas>
                    <asp:ChartArea Name="ChartArea1">
                        <AxisX IntervalAutoMode="VariableCount">
                        </AxisX>
                    </asp:ChartArea>
                </ChartAreas>
                <Legends>
                    <asp:Legend Name="Legend1">
                    </asp:Legend>
                </Legends>
            </asp:Chart>
        </div>
        <div class="col-md-6">
            <asp:Chart ID="Chart2" runat="server" Height="6000px" Width="500px">
                <Series>
                    <asp:Series Name="Series1" ChartType="StackedBar" Legend="Legend1" LegendText="OK"></asp:Series>
                    <asp:Series ChartArea="ChartArea1" ChartType="StackedBar" Legend="Legend1" LegendText="RE" Name="Series2">
                    </asp:Series>
                    <asp:Series ChartArea="ChartArea1" ChartType="StackedBar" Legend="Legend1" LegendText="NG" Name="Series3">
                    </asp:Series>
                </Series>
                <ChartAreas>
                    <asp:ChartArea Name="ChartArea1">
                        <AxisX IntervalAutoMode="VariableCount">
                        </AxisX>
                    </asp:ChartArea>
                </ChartAreas>
                <Legends>
                    <asp:Legend Name="Legend1">
                    </asp:Legend>
                </Legends>
            </asp:Chart>
        </div>
    </div>

</asp:Content>
