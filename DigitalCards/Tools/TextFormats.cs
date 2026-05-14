using System;
using System.IO;

public class TextFormats
{
    private static Random random = new Random();

    public static string GenerateRandomText()
    {
        int length = 10;
        const string PossibleCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        char[] randomText = new char[length];

        for (int i = 0; i < length; i++)
        {
            int randomNumber = random.Next(PossibleCharacters.Length);
            randomText[i] = PossibleCharacters[randomNumber];
        }

        return new string(randomText);
    }

    public static string DeleteSpace(string input)
    {
        string result = input.Replace(" ", "");

        return result;
    }
}
