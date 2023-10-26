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
        MenuInicial();
    }

    static void MenuInicial()
    {
        Console.WriteLine($"-------------------- Gerador de Versão");

        if (!File.Exists("config.json"))
        {
            Console.WriteLine("-------------------- Arquivo config.json não encontrado.");
        }
        var json_config = File.ReadAllText("config.json");
        var config = JsonConvert.DeserializeObject<ConfigDTO>(json_config);

        Console.WriteLine("Escolha um sistema para gerar a versão:");
        for (int i = 0; i < config?.Sistemas?.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {config.Sistemas[i].Nome}");
        }
        Console.WriteLine($"--------------------");

        int escolha;
        var valor_escolha = Console.ReadLine();
        if (!int.TryParse(valor_escolha, out escolha))
        {
            Console.WriteLine("-------------------- Insira Somente números.");
            return;
        }

        if (escolha >= 1 && escolha <= config.Sistemas.Count)
        {
            var sistemaSelecionado = config.Sistemas[escolha - 1];
            GerarVersao(config.Destino, sistemaSelecionado, config.Logs ? 1 : 0, config.LimparDestinoCadaVersao ? 1 : 0);
        }
        else
        {
            Console.WriteLine("-------------------- Opção inválida.");
        }

        Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Processo encerrado com sucesso");
        Console.WriteLine("-------------------- Deseja voltar ao menu inicial? 1 SIM, 0 NÃO");
       
        int gerar_novamente;
        var valor_gerar_novamente = Console.ReadLine();
        if (!int.TryParse(valor_gerar_novamente, out gerar_novamente))
        {
            Console.WriteLine("-------------------- Insira Somente números.");
            return;
        }

        if(gerar_novamente == 1)
        {
            Console.Clear();
            MenuInicial();
        }
        else
        {
            Environment.Exit(0);
        }
    }
    static void GerarVersao(string destino, Sistema sistema, int ver_progresso, int limpar_destino_cada_versao)
    {
        Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Gerando versão: {sistema.Nome}");

        if (string.IsNullOrEmpty(destino))
        {
            Console.WriteLine($"-------------------- Informe o Destino da versão");
            return;
        }

        if (string.IsNullOrEmpty(sistema.CaminhoPublish))
        {
            Console.WriteLine($"-------------------- Informe o CaminhoPublish do sistema {sistema.Nome}");
            return;
        }

        if (string.IsNullOrEmpty(sistema.CaminhoWeb) && string.IsNullOrEmpty(sistema.CaminhoApi))
        {
            Console.WriteLine($"-------------------- Informe o CaminhoWeb e/ou CaminhoApi do sistema {sistema.Nome}");
            return;
        }

        string destino_zip = $@"{destino}\{sistema.Nome.ToUpper().Replace(" ", "")}";
        if (!Directory.Exists(destino_zip))
        {
            Directory.CreateDirectory(destino_zip);
        }

        if (limpar_destino_cada_versao == 1)
        {
            Console.WriteLine(@$"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Limpando o diretório final {destino_zip}");
            if (Directory.Exists(destino_zip))
            {
                Directory.GetFiles(destino_zip).ToList().ForEach(File.Delete);
            }
        }

        if (!string.IsNullOrEmpty(sistema.CaminhoWeb))
        {
        
            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Criando versão WEB, origem: {sistema.CaminhoWeb}");
            if (sistema.RodarNpm)
            {
                Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Rodando npm run build");
                RodarComando("npm run build", sistema.CaminhoWeb, ver_progresso);
            }

            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Deletando pasta release");
            DeletarRelease($@"{sistema.CaminhoWeb}{sistema.CaminhoPublish}");
 
            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Rodando dotnet publish -c release");
            RodarComando("dotnet publish -c release", sistema.CaminhoWeb, ver_progresso);

            Console.WriteLine(@$"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Enviando os arquivos para o diretório final {destino_zip}\PUBLISH_WEB_{DateTime.Now.ToString("ddMMyyyy")}.zip");
            EnviarParaDestino(destino_zip + $@"\PUBLISH_WEB_{DateTime.Now.ToString("ddMMyyyy")}.zip", $@"{sistema.CaminhoWeb}{sistema.CaminhoPublish}");

        }

        if (!string.IsNullOrEmpty(sistema.CaminhoApi))
        {
  
            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Criando versão API, origem: {sistema.CaminhoApi}");

            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Deletando pasta release");
            DeletarRelease($@"{sistema.CaminhoApi}{sistema.CaminhoPublish}");

            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Rodando dotnet publish -c release");
            RodarComando("dotnet publish -c release", sistema.CaminhoApi, ver_progresso);

            Console.WriteLine(@$"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Enviando os arquivos para o diretório {destino_zip}\PUBLISH_API_{DateTime.Now.ToString("ddMMyyyy")}.zip");
            EnviarParaDestino(destino_zip + $@"\PUBLISH_API_{DateTime.Now.ToString("ddMMyyyy")}.zip", $@"{sistema.CaminhoApi}{sistema.CaminhoPublish}");
        }
    }
    static void RodarComando(string comando, string path, int ver_progresso)
    {
        try
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + comando)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = path
            };

            var process = Process.Start(processInfo);

            if (ver_progresso == 1)
            {
                process.OutputDataReceived += (sender, data) =>
                {
                    if (data.Data != null)
                    {
                        Console.WriteLine(data.Data);
                    }
                };
                process.ErrorDataReceived += (sender, data) =>
                {
                    if (data.Data != null)
                    {
                        Console.WriteLine(data.Data);
                    }
                };
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Erro ao executar o comando: {ex}");
        }

    }
    static void DeletarRelease(string caminho_publish)
    {
        try
        {
            string pasta_release = Path.GetFullPath(Path.Combine(caminho_publish, @"..\..")); 
            if (Directory.Exists(pasta_release))
            {
                Directory.Delete(pasta_release, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Erro ao deletar a pasta release: {ex}");
        }
    }
    static void EnviarParaDestino(string destino_zip, string origem_zip)
    {
        try
        {
            if (File.Exists(destino_zip))
            {
                File.Delete(destino_zip);
            }

            var data_inicio = DateTime.Now.ToString("HH:mm:ss");
            var files = Directory.GetFiles(origem_zip, "*", SearchOption.AllDirectories);
            var totalCount = files.Length;
            var currentCount = 0;

            using (FileStream zipToOpen = new FileStream(destino_zip, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        currentCount++;

                        var relativePath = file.Substring(origem_zip.Length + 1); 
                        archive.CreateEntryFromFile(file, relativePath);

                        double percentage = (double)currentCount / totalCount * 100;
                        string displayedFilename = relativePath.Length > 70 ? relativePath.Substring(0, 67) + "..." : relativePath; 
                        Console.Write($"\r-------------------- {data_inicio} Progresso enviando os arquivos para o diretório final: {percentage:0.00}% - Arquivo: {displayedFilename,-70}"); 
                    }
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"-------------------- {DateTime.Now.ToString("HH:mm:ss")} Erro ao enviar os arquivos para o diretório final: {ex}");
        }
    }
}