namespace EventViewerX;

/// <summary>
/// Identifies the type of network access server port from the RADIUS NAS-Port-Type attribute.
/// </summary>
public enum NasPortType {
    Async = 0,
    Sync = 1,
    ISDN = 2,
    ISDNV120 = 3,
    ISDNV110 = 4,
    Virtual = 5,
    PIAFS = 6,
    HdlcClearChannel = 7,
    X25 = 8,
    X75 = 9,
    G3Fax = 10,
    SDSL = 11,
    ADSLCAP = 12,
    ADSLDMT = 13,
    IDSL = 14,
    Ethernet = 15,
    XDSL = 16,
    Cable = 17,
    WirelessOther = 18,
    WirelessIEEE80211 = 19,
    TokenRing = 20,
    FDDI = 21,
    WirelessCDMA = 22,
    WirelessCDPD = 23,
    WirelessIDEN = 24,
    Wireless1XEV = 25,
    IAPP = 26,
    FTTP = 27
}
