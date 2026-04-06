using System.Collections.Generic;

namespace com.shepherdchurch.DigitalSignage.ViewModels;

public class DigitalSignRotatorConfigurationBag
{
    public string ErrorMessage { get; set; }

    public int? DeviceId { get; set; }

    public int? SlideInterval { get; set; }

    public int? UpdateInterval { get; set; }

    public List<string> Transitions { get; set; }

    public int? ContentChannelId { get; set; }

    public bool IsAudioEnabled { get; set; }
}
