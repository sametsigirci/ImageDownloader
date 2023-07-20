using Domain.Utilities;

namespace Domain.Interfaces
{
    public interface IInputReader
    {
        Input ReadInputFromJson(string inputFilePath);
        Input ReadInputFromConsole(int count,int parallelism,string savePath);
    }
}
