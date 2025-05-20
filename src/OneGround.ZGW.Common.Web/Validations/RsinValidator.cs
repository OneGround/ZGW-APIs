namespace OneGround.ZGW.Common.Web.Validations;

public static class RsinValidator
{
    public static bool ValidateElfProef(string rsin)
    {
        if (rsin.Length != 9)
            return false;
        int i = 9;

        int total = 0;
        for (int j = 0; j < 8; j++)
        {
            char t = rsin[j];
            total += int.Parse(t.ToString()) * i;
            i--;
        }
        int rest = int.Parse(rsin[8].ToString());
        return (total % 11) == rest;
    }
}
