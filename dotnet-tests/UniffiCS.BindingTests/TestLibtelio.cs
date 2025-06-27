using uniffi.telio;
using System;

namespace UniffiCS.BindingTests;


class OurEventCb : TelioEventCb
{
    public void DoNothing() { }

    public void Event(Event payload) 
    {
        switch (payload)
        {
            case Event.Node node:
                {
                    // Try to access the data
                    if (node.body.isExit == false) {
                        DoNothing();
                    }

                    if (node.body.hostname == "Some hostname") {
                        DoNothing();
                    }
                    break;
                }
            case Event.Relay relay:
                {
                    // Try to access the data
                    if (relay.body.regionCode == "Some region") {
                        DoNothing();
                    }

                    if (relay.body.usePlainText == false) {
                        DoNothing();
                    }
                    break;
                }
            case Event.Error error:
                {
                    // Try to access the data
                    if (error.body.msg == "Test error") {
                        DoNothing();
                    }
                    break;
                }
        }
    }
}

public class TestLibtelio
{
    [Fact]
    public void TestTelio()
    {
        var features = new FeaturesDefaultsBuilder().Build();
        var callback = new OurEventCb();
        var t = new Telio(features, callback);
    }
}
