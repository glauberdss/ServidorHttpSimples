using System.Net;
using System.Net.Sockets;
using System.Text;

class ServidorHttp
{
    private TcpListener Controlador { get; set; }

    private int Porta { get; set; }

    private int QtdeRequests { get; set; }

    public string HtmlExemplo { get; set; }

    private SortedList<string,string> TiposMIme {get; set;}

    public ServidorHttp(int porta = 8080)
    {
        this.Porta = porta;
        this.CriarHtmlExemplo();
        this.PopularTiposMIME();
        try
        {
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor HTTP esta rodando na porta {this.Porta}.");
            Console.WriteLine($"Para Acessar digite no navegador: http://localhost:{this.Porta}.");
            Task servidorHttpTask = Task.Run(() => AguardarRequests());
            servidorHttpTask.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao iniciar servidor na porta {this.Porta}:\n{e.Message}");
        }
    }


    private async Task AguardarRequests()
    {
        while (true)
        {
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdeRequests));
        }
    }


    private void ProcessarRequest(Socket conexao, int numeroRequest)
    {
        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if (conexao.Connected)
        {
            byte[] bytesRequisicao = new byte[1024];
            conexao.Receive(bytesRequisicao, bytesRequisicao.Length, 0);
            string textoRequisicao = Encoding.UTF8.GetString(bytesRequisicao).Replace((char)0, ' ').Trim();
            if (textoRequisicao.Length > 0)
            {
                Console.WriteLine($"\n{textoRequisicao}\n");

                //captura o metodoHttp, recursoBuscado, versaoHttp na linha 1 do texto de requisição do navegador.
                string[] linhas = textoRequisicao.Split("\r\n");
                int iPrimeiroEspaco = linhas[0].IndexOf(' ');
                int iSegundoEspaco = linhas[0].LastIndexOf(' ');
                string  metodoHttp = linhas[0].Substring(0, iPrimeiroEspaco);
                string recursoBuscado = linhas[0].Substring(iPrimeiroEspaco + 1, iSegundoEspaco - iPrimeiroEspaco-1);
                string versaoHttp = linhas[0].Substring(iSegundoEspaco + 1);
                
                // Captura o nome do host na segunda linha do texto da requisição do navegador.
                iPrimeiroEspaco = linhas[1].IndexOf(' ');
                string nomeHost =  linhas[1].Substring(iPrimeiroEspaco + 1);

                byte[] bytesCabecalho = null;
                byte[] bytesConteudo = null;
                FileInfo fiArquivo = new FileInfo(ObterCaminhoFisicoArquivo(recursoBuscado));
                if (fiArquivo.Exists)
                {                
                    if(TiposMIme.ContainsKey(fiArquivo.Extension.ToLower()))
                    {
                        bytesConteudo = File.ReadAllBytes(fiArquivo.FullName);
                        string tipoMime = TiposMIme[fiArquivo.Extension.ToLower()];
                        bytesCabecalho = GerarCabecalho(versaoHttp, tipoMime, "200", bytesConteudo.Length);
                    }
                    else
                    {
                        bytesConteudo = Encoding.UTF8.GetBytes("<h1>Erro 415 - Tipo de Arquivo não suportado</h1>");
                        bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf8", "415",bytesConteudo.Length);
                    }
                }
                else
                {
                    bytesConteudo = Encoding.UTF8.GetBytes("<h1>Erro 404 - Arquivo não encontrado</h1>");
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", "400", bytesConteudo.Length); 
                }

                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);
                conexao.Close();
                Console.WriteLine($"\n{bytesEnviados} bytes enviados em resposta à requisição #{numeroRequest}.");
            }
        }
        Console.WriteLine($"\nRequest {numeroRequest} finalizado.");
    }


    public byte[] GerarCabecalho(string versaoHttp, string tipoMime, string codigoHttp, int qtdeBytes = 0)
    {
        StringBuilder texto = new StringBuilder();
        texto.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
        texto.Append($"Server: Servidor Http Simples 1.0{Environment.NewLine}");
        texto.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
        texto.Append($"Content-Length: {qtdeBytes}{Environment.NewLine}{Environment.NewLine}");
        return Encoding.UTF8.GetBytes(texto.ToString());
    }


    private void CriarHtmlExemplo()
    {
        StringBuilder html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"pt-br\"><head><meta charset=\"UTF-8\">");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.Append("<title>Página Estatica</title></head><body>");
        html.Append("<h1>Página Estática</h1></body></html>");
        this.HtmlExemplo = html.ToString();
    }


    public byte[] LerArquivo(string recurso)
    {
        string diretorio = "C:\\Dev\\dotNet_C#\\ServidorHttpSimples\\www";
        string caminhoArquivo = diretorio + recurso.Replace("/", "\\");
        if (File.Exists(caminhoArquivo))
        {
            return File.ReadAllBytes(caminhoArquivo);
        }
        else return new byte[0];
    }
    
    private void PopularTiposMIME()
    {
        this.TiposMIme = new SortedList<string, string>();
        this.TiposMIme.Add(".html", "text/html;charset=utf-8");
        this.TiposMIme.Add(".htm", "text/html;charset=utf-8");
        this.TiposMIme.Add(".css","text/css");
        this.TiposMIme.Add(".js","text/javascript");
        this.TiposMIme.Add(".png","image/png");
        this.TiposMIme.Add(".jpg","image/jpeg");
        this.TiposMIme.Add(".gif","image/gif");
        this.TiposMIme.Add(".svg","image/svg+xml");
        this.TiposMIme.Add(".webp","image/webp");
        this.TiposMIme.Add(".ico","image/ico");
        this.TiposMIme.Add(".woff","font/woff");
        this.TiposMIme.Add(".woff2","font/woff2");
    }

    public string ObterCaminhoFisicoArquivo(string arquivo)
    {
        string caminhoArquivo = "C:\\Dev\\dotNet_C#\\ServidorHttpSimples\\www" + arquivo.Replace("/", "\\");
        return caminhoArquivo;
    }
    
    // Parei em 08:45min
    // link do video: https://www.youtube.com/watch?v=_3_tDDpzS30&list=PLryiSJEgIl1vOhiSJ2fyjfpsuwjB-bfde&index=6&t=78s

}