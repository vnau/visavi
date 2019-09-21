using Visavi;

namespace Visavi.Example.PNA
{
    class Program
    {
        static void Main(string[] args)
        {
            double FreqStart = 1000000000;
            double FreqStop = 2000000000;

            var vi = new MessageSession(null).WithErrorsCheck();

            try
            {
                // Reset the analyzer to instrument preset
                vi.Print("SYSTem:FPRESET");

                // Create S11 measurement
                vi.Print("CALCulate1:PARameter:DEFine:EXT 'My_S11',S11");

                // Turn on Window #1
                vi.Print("DISPlay:WINDow1:STATe {0}", true);

                // Put a trace (Trace #1) into Window #1 and 'feed' it from the measurement
                vi.Print("DISPlay:WINDow1:TRACe1:FEED 'My_S11'");

                // Setup the channel for single sweep trigger
                vi.WithTimeout(1000).Query<int>("INITiate1:CONTinuous OFF;*OPC?");

                vi.Print("SENSe1:SWEep:TRIGger:POINt OFF");

                // Set channel parameters
                vi.Print("SENSe1:SWEep:POINts {0}", 11);
                vi.Print("SENSe1:FREQuency:STARt {0}", FreqStart);
                vi.Print("SENSe1:FREQuency:STOP {1}", FreqStop);

                // Send a trigger to initiate a single sweep
                vi.WithTimeout(1000).Query<int>("INITiate1;*OPC?");

                // Must select the measurement before we can read the data
                vi.Print("CALCulate1:PARameter:SELect 'My_S11'");

                // Read the measurement data into the "result" string variable
                vi.Print("FORMat ASCII");
                var data = vi.Query<double[]>("CALCulate1:DATA? FDATA");
            }
            catch (ScpiErrorException e)
            {
                System.Console.WriteLine("SCPI error occured for command \"{0}\": {1}", e.Context, e.Message);
            }
        }
    }
}
