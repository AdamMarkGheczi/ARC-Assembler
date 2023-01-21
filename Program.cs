using System.Text.RegularExpressions;

StreamReader reader = new StreamReader(@"..\..\..\Input.txt");

int locationCounter = 0;
List<string> symbols = new List<string>();
List<int> symbolValues = new List<int>();

string line;

// initial parse for symbols

while((line = reader.ReadLine()) != null)
{
    // remove comments
    line = Regex.Replace(line, @"\s*!.*", "");
    line = line.Trim();

    if(line != "")
    {
        if (!line.Contains('.'))
        {
            if (line.Contains(':'))
            {
                string[] tokens = line.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                symbols.Add(tokens[0]);
                symbolValues.Add(locationCounter);
            }
            locationCounter += 4;
        }
        else
        {
            // split on commas and/or whitespaces
            Regex rex = new Regex(@",*\s+");

            string[] tokens = rex.Split(line);

            if (tokens.Contains(".equ"))
            {
                symbols.Add(tokens[0]);
                symbolValues.Add(int.Parse(tokens[2]));
            }

            if (tokens.Contains(".org"))
            {
                int number;
                if (int.TryParse(tokens[1], out number))
                    locationCounter = number;
                else
                    locationCounter = symbolValues[symbols.IndexOf(tokens[1])];
            }

            if (tokens.Contains(".end"))
            {
                break;
            }

        }
    }

}

Console.WriteLine();

reader = new StreamReader(@"..\..\..\Input.txt");
StreamWriter writer = new StreamWriter(@"..\..\..\Output.txt");

// seconds parse, for the instructions
while ((line = reader.ReadLine()) != null)
{
    // remove comments
    line = Regex.Replace(line, @"\s*!.*", "");
    line = line.Trim();

    if (line != "")
    {
        if (!line.Contains('.'))
        {
            // remove label
            if (line.Contains(":")) line = Regex.Replace(line, @"\w+:\s*", "");

            // split on commas and/or whitespaces
            Regex rex = new Regex(@",*\s+");
            string[] tokens = rex.Split(line);

            writer.WriteLine(translateInstruction(tokens));
            locationCounter += 4;
        }
        else
        {
            // split on commas and/or whitespaces
            Regex rex = new Regex(@",*\s+");

            string[] tokens = rex.Split(line);

            if (tokens.Contains(".org"))
            {
                int number;
                if (int.TryParse(tokens[1], out number))
                    locationCounter = number;
                else
                    locationCounter = symbolValues[symbols.IndexOf(tokens[1])];
            }

            if (tokens.Contains(".end"))
            {
                break;
            }

        }
    }

}

string translateInstruction(string[] tokens)
{
    int TryParseNumberVariable;
    string mc = "";
    
    // checking for 0x prefixed hexadecimal numbers
    Regex hexPrefix = new Regex(@"^0x");

    // just a standalone number case
    if (tokens.Length == 1)
    {
        string temp;
        if (symbols.Contains(tokens[0]))
        {
            temp = symbolValues[symbols.IndexOf(tokens[0])].ToString();
        }
        else
            temp = tokens[0];

        mc += ConvertToBinaryString(temp, 32);
        return mc;
    }

    string opc = op1(tokens[0]);
    mc += opc;

    string rs1 = "", rs2 = "", rd, simm13 = "";

    switch (opc)
    {
        case "00":
            if (tokens[0] == "sethi")
            {
                mc += ConvertToBinaryString(strip(tokens[2]), 5);
                mc += ConvertToBinaryString(tokens[1], 22);
            }
            else
            {
                mc += '0';
                mc += cond(tokens[0]);
                mc += op2(tokens[0]);

                if (int.TryParse(tokens[1], out TryParseNumberVariable))
                    mc += ConvertToBinaryString(((TryParseNumberVariable - locationCounter) / 4).ToString(), 22);
                else
                    mc += ConvertToBinaryString(((symbolValues[symbols.IndexOf(tokens[1])] - locationCounter) / 4).ToString(), 22);
            }
            break;

        case "01":
            if (int.TryParse(tokens[1], out TryParseNumberVariable))
                mc += ConvertToBinaryString((TryParseNumberVariable - locationCounter).ToString(), 30);
            else
                mc += ConvertToBinaryString((symbolValues[symbols.IndexOf(tokens[1])] - locationCounter).ToString(), 30);
            break;
        case "10":
            rs1 = ""; rs2 = ""; rd = ""; simm13 = "";
            rd = ConvertToBinaryString(strip(tokens[tokens.Length - 1]), 5);
            mc += rd;
            mc += op3(tokens[0]);


            for (int i = 1; i < tokens.Length - 1 ; i++)
            {
                if (Regex.IsMatch(tokens[i], @"\s*\+\s*"))
                {
                    rs1 = Regex.Split(tokens[i], @"\s*\+\s*")[0];
                    simm13 = Regex.Split(tokens[i], @"\s*\+\s*")[1];

                    rs1 = strip(rs1);
                    if (symbols.Contains(rs1)) rs1 = ConvertToBinaryString(symbolValues[symbols.IndexOf(rs1)].ToString(), 5);
                    else rs1 = ConvertToBinaryString(rs1, 5);

                    simm13 = strip(simm13);
                    if (symbols.Contains(simm13)) simm13 = ConvertToBinaryString(symbolValues[symbols.IndexOf(simm13)].ToString(), 13);
                    else simm13 = ConvertToBinaryString(simm13, 13);

                    break;
                }

                if (Regex.IsMatch(tokens[i], @"\[\w*\]"))
                {
                    simm13 = strip(tokens[i]);
                    simm13 = ConvertToBinaryString(symbolValues[symbols.IndexOf(simm13)].ToString(), 13);
                }
                else
                {
                    if (rs1 == "") rs1 = ConvertToBinaryString(strip(tokens[i]), 5);
                    else if(simm13 == "") rs2 = ConvertToBinaryString(strip(tokens[i]), 5);
                }
            }

            if (rs1 == "") rs1 = "00000";
            if (rs2 == "") rs2 = "00000";
            if (simm13 != "")
                    mc += rs1 + '1' + simm13;
                else
                    mc += rs1 + '0' + "00000000" + rs2;

            break;

        case "11":
                rs1 = ""; rs2 = ""; rd = ""; simm13 = "";

                if (tokens[0] == "ld")
                    rd = ConvertToBinaryString(strip(tokens[tokens.Length - 1]), 5);
                else rd = ConvertToBinaryString(strip(tokens[1]), 5);
                
                mc += rd;
                mc += op4(tokens[0]);


                for (int i = tokens[0] == "ld" ? 1 : 2; i < tokens.Length; i++)
                {
                    if (Regex.IsMatch(tokens[i], @"\s*\+\s*"))
                    {
                        rs1 = Regex.Split(tokens[i], @"\s*\+\s*")[0];
                        simm13 = Regex.Split(tokens[i], @"\s*\+\s*")[1];

                        rs1 = strip(rs1);
                        if (symbols.Contains(rs1)) rs1 = ConvertToBinaryString(symbolValues[symbols.IndexOf(rs1)].ToString(), 5);
                        else rs1 = ConvertToBinaryString(rs1, 5);

                        simm13 = strip(simm13);
                        if (symbols.Contains(simm13)) simm13 = ConvertToBinaryString(symbolValues[symbols.IndexOf(simm13)].ToString(), 13);
                        else simm13 = ConvertToBinaryString(simm13, 13);

                        break;
                    }

                    if (Regex.IsMatch(tokens[i], @"\[\w*\]"))
                    {
                        simm13 = strip(tokens[i]);
                        simm13 = ConvertToBinaryString(symbolValues[symbols.IndexOf(simm13)].ToString(), 13);
                    }
                    else
                    {
                        if (rs1 == "") rs1 = ConvertToBinaryString(strip(tokens[i]), 5);
                        else if (simm13 == "") rs2 = ConvertToBinaryString(strip(tokens[i]), 5);
                    }
                }

                if (rs1 == "") rs1 = "00000";
                if (rs2 == "") rs2 = "00000";

                if (simm13 != "")
                    mc += rs1 + '1' + simm13;
                else
                    mc += rs1 + '0' + "00000000" + rs2;
            break;
    }

    return mc;
}

string strip(string expr)
{
    expr = Regex.Replace(expr, @"\[|\]", "");
    expr = Regex.Replace(expr, @"%r", "");
    return expr;
}

string ConvertToBinaryString(string number, int length)
{
    Regex hexPrefix = new Regex(@"^0x");
    string result = "";

    if (hexPrefix.IsMatch(number))
        result = Convert.ToString(Convert.ToUInt32(number, 16), 2);
    else
        result = Convert.ToString(int.Parse(number), 2);

    result = result.PadLeft(32, '0');

    if (result.Length > length)
        result = result.Remove(0, result.Length - length);

    return result;
}

string op1(string inst)
{
    switch (inst)
    {
        case "sethi":
        case "be":
        case "bcs":
        case "bneg":
        case "bnvs":
        case "ba":
            return "00";
            
        case "call":
            return "01";

        case "addcc":
        case "andcc":
        case "orcc":
        case "orncc":
        case "srl":
        case "jmpl":
            return "10";

        case "ld":
        case "st":
            return "11";
    }

    return "";
}

string op2(string inst)
{
    switch (inst)
    {
        case "be":
        case "bcs":
        case "bneg":
        case "bvs":
        case "ba":
            return "010";

        case "sethi":
            return "100";
    }

    return "";
}

string op3(string inst)
{
    switch (inst)
    {
        case "addcc":
            return "010000";
        case "andcc":
            return "010001";
        case "orcc":
            return "010010";
        case "orncc":
            return "010110";
        case "srl":
            return "100110";
        case "jmpl":
            return "111000";
    }

    return "";
}

string op4(string inst)
{
    switch (inst)
    {
        case "ld":
            return "000000";

        case "st":
            return "000100";
    }

    return "";
}

string cond(string inst)
{
    switch (inst)
    {
        case "be":
            return "0001";
        case "bcs":
            return "0101";
        case "bneg":
            return "0110";
        case "bvs":
            return "0111";
        case "ba":
            return "1000";
    }

    return "";
}


writer.Close();
Console.WriteLine("Assembly done, \"Output.txt\" generated. Press any key to exit!");
Console.ReadKey();