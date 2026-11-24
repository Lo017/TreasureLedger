namespace TreasureLedger.Data;

public static class TreasureMapCatalog
{
    public static string GetGrade(uint rank)
    {
        return rank switch
        {
            1 => "G1",
            2 => "G2",
            3 => "G3",
            4 => "G4",
            5 => "G5",
            9 => "G6",
            10 => "G7",
            11 => "G8",
            12 => "G9",
            13 => "G10",
            17 => "G11",
            18 => "G12",
            21 => "G13",
            22 => "G14",
            24 => "G15",
            27 => "G16",
            28 => "G17",
            30 => "G18",
            _ => string.Empty
        };
    }
}
