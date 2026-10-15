using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace UTAUTalkVoice;

internal static class PitchEditorTheme
{
    public static void Apply(Control panel)
    {
        panel.Background = new SolidColorBrush(Color.FromRgb(36, 36, 36));
        panel.Foreground = Brushes.WhiteSmoke;
        panel.FontSize = 12;
        // Local templates prevent the host's implicit styles from mixing light
        // surfaces with dark-theme text. All colors stay scoped to this editor.
        panel.Resources = (ResourceDictionary)XamlReader.Parse("""
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Style TargetType="TextBlock">
                <Setter Property="Foreground" Value="#E2E2E2"/>
              </Style>
              <Style TargetType="TextBox">
                <Setter Property="Foreground" Value="#EAEAEA"/>
                <Setter Property="Background" Value="#303030"/>
                <Setter Property="BorderBrush" Value="#555555"/>
                <Setter Property="CaretBrush" Value="#FFFFFF"/>
                <Setter Property="Padding" Value="6,4"/>
                <Setter Property="Template">
                  <Setter.Value><ControlTemplate TargetType="TextBox">
                    <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1">
                      <ScrollViewer x:Name="PART_ContentHost" Margin="{TemplateBinding Padding}"/>
                    </Border>
                  </ControlTemplate></Setter.Value>
                </Setter>
              </Style>
              <Style TargetType="Button">
                <Setter Property="Foreground" Value="#EAEAEA"/>
                <Setter Property="Background" Value="#3B3B3B"/>
                <Setter Property="Padding" Value="10,5"/>
                <Setter Property="Template">
                  <Setter.Value><ControlTemplate TargetType="Button">
                    <Border x:Name="Surface" Background="{TemplateBinding Background}" BorderBrush="#565656" BorderThickness="1" Padding="{TemplateBinding Padding}">
                      <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                      <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Background" Value="#505050"/></Trigger>
                      <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="Background" Value="#505050"/></Trigger>
                      <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.4"/></Trigger>
                    </ControlTemplate.Triggers>
                  </ControlTemplate></Setter.Value>
                </Setter>
              </Style>
              <Style TargetType="Slider">
                <Setter Property="Template">
                  <Setter.Value><ControlTemplate TargetType="Slider">
                    <Grid Width="40" Background="Transparent">
                      <Border Width="2" Background="#696969" Margin="0,6"/>
                      <Track x:Name="PART_Track" Orientation="Vertical" Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}" Value="{TemplateBinding Value}">
                        <Track.DecreaseRepeatButton><RepeatButton Command="Slider.DecreaseLarge" Focusable="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
                        <Track.Thumb><Thumb Width="27" Height="12"><Thumb.Template><ControlTemplate TargetType="Thumb"><Border Background="#8A8A8A" BorderBrush="#B3B3B3" BorderThickness="1"/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
                        <Track.IncreaseRepeatButton><RepeatButton Command="Slider.IncreaseLarge" Focusable="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
                      </Track>
                    </Grid>
                  </ControlTemplate></Setter.Value>
                </Setter>
              </Style>
              <Style TargetType="ScrollBar">
                <Setter Property="Background" Value="#2B2B2B"/>
                <Setter Property="Template">
                  <Setter.Value><ControlTemplate TargetType="ScrollBar">
                    <Grid Background="{TemplateBinding Background}" MinWidth="12" MinHeight="12">
                      <Track x:Name="PART_Track" Orientation="{TemplateBinding Orientation}" Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}" Value="{TemplateBinding Value}" ViewportSize="{TemplateBinding ViewportSize}" IsDirectionReversed="True">
                        <Track.Thumb><Thumb MinWidth="12" MinHeight="12"><Thumb.Template><ControlTemplate TargetType="Thumb"><Border Background="#646464" Margin="2"/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
                      </Track>
                    </Grid>
                    <ControlTemplate.Triggers>
                      <Trigger Property="Orientation" Value="Horizontal"><Setter TargetName="PART_Track" Property="IsDirectionReversed" Value="False"/></Trigger>
                    </ControlTemplate.Triggers>
                  </ControlTemplate></Setter.Value>
                </Setter>
              </Style>
            </ResourceDictionary>
            """);
    }
}
