using GeradorVersao.DTOs;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"-------------------- Gerador de Versão");

        if (!File.Exists("config.json"))
        {
            Console.WriteLine("-------------------- Arquivo config.json não encontrado.");
        }
        var config = File.ReadAllText("config.json");
        var lista_sistemas = JsonConvert.DeserializeObject<ListaSistema>(config);

        Console.WriteLine("Escolha um sistema para gerar a versão:");
        for (int i = 0; i < lista_sistemas?.Sistemas?.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {lista_sistemas.Sistemas[i].Nome}");
        }
        Console.WriteLine($"--------------------");

        int escolha;
        var valor_escolha = Console.ReadLine();
        if (!int.TryParse(valor_escolha, out escolha))
        {
            Console.WriteLine("-------------------- Insira Somente números.");
            return;
        }

        if (escolha >= 1 && escolha <= lista_sistemas?.Sistemas?.Count)
        {
            Console.WriteLine("-------------------- Deseja ver o retorno dos comandos executados? 1 SIM, 0 NÃO");
            int ver_progresso;
            var valor_ver_progresso = Console.ReadLine();
            if (!int.TryParse(valor_ver_progresso, out ver_progresso))
            {
                Console.WriteLine("-------------------- Insira Somente números.");
                return;
            }

            var sistemaSelecionado = lista_sistemas.Sistemas[escolha - 1];
            GerarVersao(lista_sistemas.Destino, sistemaSelecionado, ver_progresso);
        }
        else
        {
            Console.WriteLine("-------------------- Opção inválida.");
        }

        Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Programa encerrado");
        Console.ReadKey();
    }

    static void GerarVersao(string destino, Sistema sistema, int ver_progresso)
    {
        Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Gerando versão: {sistema.Nome}");

        string destino_zip = $@"{destino}\{sistema.Nome.ToUpper().Replace(" ", "")}";
        if (!Directory.Exists(destino_zip))
        {
            Directory.CreateDirectory(destino_zip);
        }

        if (string.IsNullOrEmpty(sistema.CaminhoPublish))
        {
            Console.WriteLine($"-------------------- Informe o CaminhoPublish do sistema {sistema.Nome}");
            return;
        }

        if (string.IsNullOrEmpty(sistema.CaminhoWeb) && string.IsNullOrEmpty(sistema.CaminhoApi))
        {
            Console.WriteLine($"-------------------- Informe o CaminhoWeb e CaminhoApi do sistema {sistema.Nome}");
            return;
        }

        if (!string.IsNullOrEmpty(sistema.CaminhoWeb))
        {
            try
            {
                Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Criando versão WEB, origem: {sistema.CaminhoWeb}");
                if (sistema.RodarNpm)
                {
                    Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Rodando npm run build");
                    RodarComandoConsole("npm run build", sistema.CaminhoWeb, ver_progresso);
                }
                Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Rodando dotnet publish -c release");

                var caminho_publish = $@"{sistema.CaminhoWeb}{sistema.CaminhoPublish}";
                string pasta_release = Path.GetFullPath(Path.Combine(caminho_publish, @"..\..")); //deleta a pasta release
                if (Directory.Exists(pasta_release))
                {
                    Directory.Delete(pasta_release, true);
                }
                RodarComando("dotnet publish -c release", sistema.CaminhoWeb, ver_progresso);

                Console.WriteLine(@$"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Enviando os arquivos para o diretório final {destino_zip}\PUBLISH_WEB.zip");
                EnviarParaDestino(destino_zip + $@"\PUBLISH_WEB.zip", $@"{sistema.CaminhoWeb}{sistema.CaminhoPublish}");
            }
            catch(Exception ex) 
            {
                Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Erro ao gerar versão WEB: {ex}");
            }

        }

        if (!string.IsNullOrEmpty(sistema.CaminhoApi))
        {
            try
            {
                Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Criando versão API, origem: {sistema.CaminhoApi}");
                Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Rodando dotnet publish -c release");

                var caminho_publish = $@"{sistema.CaminhoApi}{sistema.CaminhoPublish}";
                string pasta_release = Path.GetFullPath(Path.Combine(caminho_publish, @"..\..")); //deleta a pasta release
                if (Directory.Exists(pasta_release))
                {
                    Directory.Delete(pasta_release, true);
                }
                RodarComando("dotnet publish -c release", sistema.CaminhoApi, ver_progresso);

                Console.WriteLine(@$"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Enviando os arquivos para o diretório final {destino_zip}\PUBLISH_API.zip");
                EnviarParaDestino(destino_zip + $@"\PUBLISH_API.zip", $@"{sistema.CaminhoApi}{sistema.CaminhoPublish}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Erro ao gerar versão API: {ex}");
            }
        }
    }

    static void RodarComando(string comando, string path, int ver_progresso)
    {
        var split = comando.Split(' ', 2);
        var fileName = split[0];
        var arguments = split.Length > 1 ? split[1] : "";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = path
            }
        };
        process.Start();
        if (ver_progresso == 1)
        {
            Console.WriteLine(process.StandardOutput.ReadToEnd());
        }
        process.WaitForExit();
    }
    static void RodarComandoConsole(string comando, string path, int ver_progresso)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {comando}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = path
            }
        };
        process.Start();
        if (ver_progresso == 1)
        {
            Console.WriteLine(process.StandardOutput.ReadToEnd());
        }
        process.WaitForExit();
    }

    static void EnviarParaDestino(string destino_zip, string origem_zip)
    {
        if (File.Exists(destino_zip))
        {
            File.Delete(destino_zip);
        }
        ZipFile.CreateFromDirectory(origem_zip, destino_zip);
    }
}