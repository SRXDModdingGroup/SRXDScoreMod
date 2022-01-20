namespace SRXDScoreMod; 

internal class PieGraphValue {
    public int Perfect { get; set; }
    
    public int Good { get; set; }
    
    public int Missed { get; set; }

    public PieGraphValue(int perfect, int good, int missed) {
        Perfect = perfect;
        Good = good;
        Missed = missed;
    }
}