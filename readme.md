# Now Playing for Playnite

This extension provides a way for a user to return to their game (similarly to BackToGame) as well as close their current game directly from Playnite

## Why use this over BackToGame

You can actually use both! But this extension does 2 main things a little better than BackToGame:
1. Finding the game process is a little more robust in my implementation as my process finder uses a variety of heuristics to find the correct game process such as:
	1. Fuzzy matching the Window's title to the Game entry
	2. Checking oif the process path matches the game's install directory (32-bit processes only due to Playnite limitations)
	3. Checking if the process name is the same as any executable inside the game's install directory (32 and 64-bit processes)
	4. Ranking multiple process matches based on system resource usage and the strength of the heuristic matches
2. You can exit your game directly from the NowPlaying window so any games that do not have good exiting functionality via a controller can still be exited easily

## Supported Themes

[See here](./supported-themes.md)

## Usage for theme Developers

### Minimal Usage

This will produce a button that only shows up if:
1. The extension is installed and
2. There is a game running

This is considered the bare minimum implementation

```xml
<ButtonEx Command="{PluginSettings Plugin=NowPlaying, Path=OpenDialog}">
  <ButtonEx.Style>
    <Style TargetType="ButtonEx"
        BasedOn="{StaticResource {x:Type Button}}">
      <Setter Property="Visibility"
          Value="Visible"/>
      <Style.Triggers>
        <DataTrigger Binding="{PluginStatus Plugin=NowPlaying_db4e7ade-57fb-426c-8392-60e2347a0209, Status=Installed}"
              	Value="False">
          <Setter Property="Visibility"
              Value="Collapsed"/>
        </DataTrigger>
        <DataTrigger Binding="{PluginSettings Plugin=NowPlaying, Path=IsGameRunning}"
              	Value="False">
          <Setter Property="Visibility"
              Value="Collapsed"/>
        </DataTrigger>
      </Style.Triggers>
    </Style>
  </ButtonEx.Style>
  <TextBox Text="Now Playing" />
</ButtonEx>
```

### Advanced usage

In addition to opening a window to control the process, the extension exposes the icon and title of the game. This button will show the currently running games's **Icon** and **Title** so encourage users to have their library's icons set.

```xml
<ButtonEx Command="{PluginSettings Plugin=NowPlaying, Path=OpenDialog}">
  <ButtonEx.Style>
    <Style TargetType="ButtonEx"
        BasedOn="{StaticResource {x:Type Button}}">
      <Setter Property="Visibility"
          Value="Visible"/>
      <Style.Triggers>
        <DataTrigger Binding="{PluginStatus Plugin=NowPlaying_db4e7ade-57fb-426c-8392-60e2347a0209, Status=Installed}"
              	Value="False">
          <Setter Property="Visibility"
              Value="Collapsed"/>
        </DataTrigger>
        <DataTrigger Binding="{PluginSettings Plugin=NowPlaying, Path=IsGameRunning}"
              	Value="False">
          <Setter Property="Visibility"
              Value="Collapsed"/>
        </DataTrigger>
      </Style.Triggers>
    </Style>
  </ButtonEx.Style>
  <StackPanel Orientation="Horizontal">
    <Image Width="40"
        Height="40"
        Style="{DynamicResource NowPlayingImageStyle}"
        Source="{PluginSettings Plugin=NowPlaying, Path=RunningGame.IconPath}"/>
    <StackPanel Orientation="Vertical">
      <TextBlock x:Name="NowPlaying_Title"
            Style="{DynamicResource NowPlayingTitleStyle}"
            Text="NOW PLAYING"/>
      <TextBlock x:Name="NowPlaying_Game"
            Style="{DynamicResource NowPlayingGameStyle}"
            Text="{PluginSettings Plugin=NowPlaying, Path=RunningGame.GameName}"/>
    </StackPanel>
  </StackPanel>
</ButtonEx>
```

## Directly Exiting or Resuming a Game

The Commands `ReturnToGame` and `CloseGame` will let you directly resume or exit a game. If you place them inside `GameDetails.xaml` you can control visibility with `Game.IsRunning` to only show controls for a running game

```xml
<ButtonEx Command="{PluginSettings Plugin=NowPlaying, Path=ReturnToGame}">
  <ButtonEx.Style>
    <Style TargetType="ButtonEx"
            BasedOn="{StaticResource {x:Type Button}}">
      <Setter Property="Visibility"
              Value="Visible"/>
      <Style.Triggers>
        <DataTrigger Binding="{Binding Game.IsRunning}"
                      Value="False">
          <Setter Property="Visibility"
                  Value="Collapsed"/>
        </DataTrigger>
      </Style.Triggers>
    </Style>
  </ButtonEx.Style>
  Return to Game
</ButtonEx>
<ButtonEx Command="{PluginSettings Plugin=NowPlaying, Path=CloseGame}">
  <ButtonEx.Style>
    <Style TargetType="ButtonEx"
            BasedOn="{StaticResource {x:Type Button}}">
      <Setter Property="Visibility"
              Value="Visible"/>
      <Style.Triggers>
        <DataTrigger Binding="{Binding Game.IsRunning}"
                      Value="False">
          <Setter Property="Visibility"
                  Value="Collapsed"/>
        </DataTrigger>
      </Style.Triggers>
    </Style>
  </ButtonEx.Style>
  Exit
</ButtonEx>
```

## Bindings
|Path|Type|Data|Notes|
|---|---|----|---|
|`OpenDialog`|Command|-|Opens the Now Playing dialog|
|`ReturnToGame`|Command|-|Directly return to the currently running game|
|`CloseGame`|Command|-|Directly close the currently running game|
|`IsGameRunning`|Boolean|`true` if a game is currently running, `false` otherwise|Used to conditionally show buttons based on a game being run|
|`RunningGame.IconPath`|String|The full URI of the currently Running Game's Icon| For visual context for whatever the currently running Game is|
|`RunningGame.GameName`|String|The full Name of the currently running Game | For display of the currently running game |
|`SessionLength`|String|The length of the current gaming session, expressed as `H:MM`| Updates every 10s. E.g. A session of length 123 minutes will be displayed as `2:03`.
---
# Special Thanks to MikeAniki and V for helping out with the testing and implementation details