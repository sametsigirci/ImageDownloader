using Domain.Utilities;
using Domain.Interfaces;
using Newtonsoft.Json;

namespace Application.Services
{
    public class InputReaderService : IInputReader
    {
        public Input ReadInputFromConsole(int count, int parallelism, string savePath)
        {
            throw new NotImplementedException();
        }

        public Input ReadInputFromJson(string inputFilePath)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("Input.json file not found.");

            var json = File.ReadAllText(inputFilePath);
            return JsonConvert.DeserializeObject<Input>(json);
        }
    }
}
