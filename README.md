# Fast Timer Load

For your time-critical operations I'd like to suggest a couple of optimizations to experiment with. The first is to avoid having to navigate to a new view. Use a OnePage architecture where views are overlapped on a common grid and the visibility is controlled by the value of `OnePageState`. This should give you very rapid switching from the main page view to what "looks like" a navigated page but really isn't. 

[![switching virtual views][1]][1]

The second is to base your timing on a `Stopwatch` and use asynchronous Task.Delay calls to update the visible display. Even if it takes a few tens of ms to switch the view, the elapsed time begins at the moment the start command is invoked so it's still accounted for. You asked for perfect synchronization, and it's important to note that it won't reach the atomic precision of measuring time with nuclear isotopes but it's really not bad. _You mentioned having an older Android so I posted a [Clone](https://github.com/IVSoftware/fast-timer-load.git) on GitHub if you'd like to see how the performance is on your device._

___

Start by making an `IValueConverter` class that returns `true` if two `enum` values are equal. Here, the default MAUI page now becomes invisible if the bound value of `OnePageState` is anything other than `OnePageState.Main`. Likewise, when the value becomes `OnePageState.Timer` the alternate virtual page becomes visible.
___

**IValueConverter**

```csharp

    class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object unk, Type targetType, object parameter, CultureInfo culture)
        {
            if (unk is Enum enum1 && parameter is Enum @enum2)
            {
                return enum1.Equals(@enum2);
            }
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
```
___
**Timer Layout Overlaps Maui Default**
 
 Add the Timer view on top of the Maui default view in a common Grid. 

```xaml   
<!--1 x 1 Grid to contain overlapping virtual page representations-->
<Grid x:Name="OnePageGrid">
    <!--Maui Default Virtual Page-->
    <ScrollView
        IsVisible="{Binding
            OnePageState, 
            Converter={StaticResource EnumToBoolConverter}, 
            ConverterParameter={x:Static local:OnePageState.Main}}">
        <VerticalStackLayout
            Spacing="25"
            Padding="30,0"
            VerticalOptions="Center">
            .
            .
            .            
    </ScrollView>
 ```

 Next, on top of the Maui default view, we stack an entirely different 'look' for the MainPage.Content.

 ```xaml       
    <!--Timer Virtual Page-->
    <Grid
        IsVisible="{Binding
            OnePageState, 
            Converter={StaticResource EnumToBoolConverter}, 
            ConverterParameter={x:Static local:OnePageState.Timer}}"
        RowDefinitions="50,*,50"
        BackgroundColor="MidnightBlue">
        <HorizontalStackLayout
            HorizontalOptions="Fill"
            BackgroundColor="White">
            <Button
                Text ="&lt;--- Main Page" 
                TextColor="CornflowerBlue"
                BackgroundColor="White"
                FontSize="Medium"
                HorizontalOptions="Start"
                Command="{Binding SetOnePageStateCommand}"
                CommandParameter="{x:Static local:OnePageState.Main}"/>
        </HorizontalStackLayout>
        <Label
            Grid.Row="1"
            Text="{Binding TimerDisplay}"
            TextColor="White"
            FontSize="48"
            FontAttributes="Bold"
            HorizontalOptions="Center"
            VerticalOptions="Center"
            HorizontalTextAlignment="Center"
            VerticalTextAlignment="Center"
            BackgroundColor="Transparent">
            <Label.GestureRecognizers>
                <TapGestureRecognizer Command="{Binding StartTimerCommand}"/>
            </Label.GestureRecognizers>
        </Label>
        <Grid
            Grid.Row="2"
            BackgroundColor="White">
            <Button
                Text ="Reset" 
                TextColor="CornflowerBlue"
                BackgroundColor="White"
                FontSize="Medium"
                HorizontalOptions="CenterAndExpand"
                Command="{Binding ResetTimerCommand}"/>
        </Grid>
    </Grid>
</Grid>
```
___

**Suggestion: Avoid using a Timer**

Here's a way to streamline updating the elapsed time without using a `Timer` instance and the resulting baggage.

```


Task _pollingTask;
private Stopwatch _stopwatch = new Stopwatch();
private async void OnStartTimer(object o)
{
    if (!_stopwatch.IsRunning)
    {
        OnePageState = OnePageState.Timer;
        try
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _stopwatch.Restart();
            while (!token.IsCancellationRequested)
            {
                var elapsed = _stopwatch.Elapsed;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TimerDisplay =
                        elapsed < TimeSpan.FromSeconds(1) ?
                        elapsed.ToString(@"hh\:mm\:ss\.ff") :
                        elapsed.ToString(@"hh\:mm\:ss");
                });
                await Task.Delay(TimeSpan.FromSeconds(0.1), token);
            }
        }
        catch { }
        finally
        {
            _stopwatch.Stop();
            _pollingTask?.Dispose();
        }
    }
}
```


  [1]: https://i.stack.imgur.com/I2FXD.png