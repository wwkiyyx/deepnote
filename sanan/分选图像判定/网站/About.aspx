<%@ Page Title="统计" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="WebApplication3.About" %>

<%@ Register Assembly="System.Web.DataVisualization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" Namespace="System.Web.UI.DataVisualization.Charting" TagPrefix="asp" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    
    <div class="jumbotron">
        <h1>
            <asp:Label ID="Label1" runat="server" Text="Label"></asp:Label>
        </h1>
        <p class="lead">
            <asp:Label ID="Label2" runat="server" Text="Label"></asp:Label>
        </p>
    </div>

    <div class="row">
        <div class="col-md-4">
            <asp:Chart ID="Chart2" runat="server">
                <Series>
                    <asp:Series Name="Series1" ChartType="Pie"></asp:Series>
                </Series>
                <ChartAreas>
                    <asp:ChartArea Name="ChartArea1"></asp:ChartArea>
                </ChartAreas>
                <Titles>
                    <asp:Title Name="Title1" Text="总量比例">
                    </asp:Title>
                </Titles>
            </asp:Chart>
        </div>
        <div class="col-md-4">
            <asp:Label ID="Label5" runat="server" Text="选择日期查看统计结果:"></asp:Label>
            <br />
            <asp:Label ID="Label6" runat="server" Text="选择左侧&quot;&gt;&quot;查看一周结果:"></asp:Label>
            <br />
            <asp:Calendar ID="Calendar1" runat="server" SelectionMode="DayWeek" OnSelectionChanged="Calendar1_SelectionChanged" Height="100%" Width="100%"></asp:Calendar>
            <br />
            <asp:Label ID="Label4" runat="server" Text="选择的日期和数量:"></asp:Label>
            <br />
            <asp:Label ID="Label3" runat="server" Text="0"></asp:Label>
        </div>
        <div class="col-md-4">
            <asp:Chart ID="Chart4" runat="server">
                <Series>
                    <asp:Series Name="Series1" ChartType="Pie"></asp:Series>
                </Series>
                <ChartAreas>
                    <asp:ChartArea Name="ChartArea1"></asp:ChartArea>
                </ChartAreas>
                <Titles>
                    <asp:Title Name="Title1" Text="所选日期比例">
                    </asp:Title>
                </Titles>
            </asp:Chart>
        </div>
        
    </div>

    <div class="row">
        <div class="col-md-12">
            <asp:Chart ID="Chart3" runat="server" Width="900px">
                <Series>
                    <asp:Series Name="ok" ChartType="Line" Legend="Legend1"></asp:Series>
                    <asp:Series ChartArea="ChartArea1" ChartType="Line" Legend="Legend1" Name="ng">
                    </asp:Series>
                    <asp:Series ChartArea="ChartArea1" ChartType="Line" Legend="Legend1" Name="re">
                    </asp:Series>
                </Series>
                <ChartAreas>
                    <asp:ChartArea Name="ChartArea1"></asp:ChartArea>
                </ChartAreas>
                <Legends>
                    <asp:Legend Name="Legend1">
                    </asp:Legend>
                </Legends>
                <Titles>
                    <asp:Title Name="Title1" Text="每日比例趋势">
                    </asp:Title>
                </Titles>
            </asp:Chart>
        </div>
    </div>

    <div class="row">
        <div class="col-md-12">
            <asp:Chart ID="Chart1" runat="server" Width="900px">
                <Series>
                    <asp:Series Name="Series1"></asp:Series>
                </Series>
                <ChartAreas>
                    <asp:ChartArea Name="ChartArea1"></asp:ChartArea>
                </ChartAreas>
                <Titles>
                    <asp:Title Name="Title1" Text="每日数量">
                    </asp:Title>
                </Titles>
            </asp:Chart>
        </div>
    </div>

</asp:Content>
