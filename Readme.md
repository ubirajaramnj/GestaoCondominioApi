Resposta da IA

IA: # 🏢 API de Gestão de Condomínio - Controle de Portaria

Uma API REST desenvolvida em .NET 8 para gerenciar autorizações de acesso, visitantes, prestadores de serviço e controle de portaria em condomínios.

A aplicação utiliza Swagger para documentação, segue uma arquitetura limpa e escalável, e oferece recursos avançados como URLs encurtadas, geração de QR codes, check-in/check-out e cálculo dinâmico de status.


🎯 Funcionalidades Principais

✅ Autorizações de Acesso - Criar, validar e gerenciar autorizações para visitantes e prestadores

✅ URLs Encurtadas - Gerar URLs seguras com expiração automática para compartilhamento

✅ Check-in/Check-out - Registrar entrada e saída de visitantes

✅ Status Dinâmico - Cálculo automático de status baseado em regras de negócio

✅ Integração WhatsApp - Envio de comprovantes via WhatsApp

✅ QR Code - Geração de QR codes para acesso rápido

✅ Documentos - Armazenamento e gerenciamento de documentos de identificação

✅ Auditoria - Log completo de todas as operações


🐳 Execução via Docker

O projeto inclui um Dockerfile otimizado para .NET 8, permitindo build e execução tanto em modo de desenvolvimento quanto de produção.

📁 Estrutura de Build em Camadas

O Dockerfile segue uma arquitetura multi-stage build, com quatro estágios:

| Estágio   | Base                                  | Função                                           |

| --------- | ------------------------------------- | ------------------------------------------------ |

| base    | mcr.microsoft.com/dotnet/aspnet:8.0 | Ambiente leve de execução (runtime).             |

| build   | mcr.microsoft.com/dotnet/sdk:8.0    | Compila e restaura dependências.                 |

| publish | mcr.microsoft.com/dotnet/sdk:8.0    | Publica o artefato final otimizado.              |

| final   | mcr.microsoft.com/dotnet/aspnet:8.0 | Imagem final com o binário pronto para produção. |


⚙️ 1️⃣ Construindo a imagem

No diretório raiz do projeto, execute:


docker build -t gestao-condominio-api -f GestaoCondominio.ControlePortaria.Api/Dockerfile .


🔹 Explicação:

 O parâmetro -f define o caminho para o Dockerfile.

 A flag -t atribui um nome à imagem (gestao-condominio-api).


🧩 2️⃣ Executando o container

Execute o container com o comando:


docker run -d -p 8090:8090 -p 8091:8091 --name condominio-api gestao-condominio-api


🔹 Isso fará com que:

 A API rode em duas portas expostas:

   8090 — acesso HTTP padrão

   8091 — porta adicional usada pelo Visual Studio / ambiente de debug

 O container rode em modo daemon (-d), permitindo execução em background.


🧭 3️⃣ Acessando a API

Após o container estar em execução, acesse:

 Swagger UI:

  👉 http://localhost:8090/swagger

 Health Check:

  👉 http://localhost:8090/health


🧹 4️⃣ Limpando containers e imagens

Para remover containers parados e imagens antigas:


docker ps -a

docker stop condominio-api

docker rm condominio-api

docker rmi gestao-condominio-api



🧠 Estrutura do Projeto


GestaoCondominio.ControlePortaria/

├── Api/

│   ├── Controllers/

│   │   ├── AutorizacoesController.cs

│   │   ├── DocumentosController.cs

│   │   ├── UrlsEncurtadasController.cs

│   │   └── VisitantesController.cs

│   └── Program.cs

├── Application/

│   ├── DTOs/

│   │   ├── AutorizacaoDto.cs

│   │   ├── UrlEncurtadaDto.cs

│   │   └── ...

│   └── Services/

│       ├── IAutorizacaoService.cs

│       ├── AutorizacaoService.cs

│       ├── IUrlEncurtadaService.cs

│       ├── UrlEncurtadaService.cs

│       └── ...

├── Domain/

│   └── Entities/

│       ├── AutorizacaoDeAcesso.cs

│       ├── UrlEncurtada.cs

│       ├── CheckInRegistro.cs

│       ├── CheckOutRegistro.cs

│       └── ...

├── Infrastructure/

│   ├── Configuration/

│   │   └── TimeZoneConfiguration.cs

│   ├── Converters/

│   │   ├── DateOnlyJsonConverter.cs

│   │   └── TimeOnlyJsonConverter.cs

│   ├── Extensions/

│   │   ├── HttpClientExtensions.cs

│   │   └── LowerCaseNamingPolicy.cs

│   └── Repositories/

│       ├── IAutorizacaoRepository.cs

│       ├── AutorizacaoRepositoryJson.cs

│       ├── IUrlEncurtadaRepository.cs

│       ├── UrlEncurtadaRepositoryJson.cs

│       └── ...

└── Data/

    ├── autorizacoes.json

    ├── urlsEncurtadas.json

    └── ...



📡 Endpoints Disponíveis

Autorizações

POST /api/autorizacoes

Criar uma nova autorização de acesso.


{

  "tipo": "Visitante",

  "periodo": "Unico",

  "nome": "Bira",

  "telefone": "11999999999",

  "dataInicio": "2025-10-28",

  "dataFim": "2025-10-30",

  "autorizador": {

    "nome": "João Silva",

    "telefone": "11988888888",

    "codigoDaUnidade": "R01-QDJ-26"

  }

}


GET /api/autorizacoes/{id}

Recuperar uma autorização específica.

GET /api/autorizacoes

Listar autorizações com filtros opcionais.


GET /api/autorizacoes?condominioId=cond-001&status=Autorizado


POST /api/autorizacoes/{id}/check-in

Registrar check-in de um visitante.


{

  "documentoId": "doc-001",

  "usuarioPortariaId": "user-001",

  "observacoes": "Entrada normal"

}


POST /api/autorizacoes/{id}/check-out

Registrar check-out de um visitante.


{

  "usuarioPortariaId": "user-001",

  "observacoes": "Saída normal"

}


POST /api/autorizacoes/{id}/cancelar

Cancelar uma autorização.

POST /api/autorizacoes/{id}/gerar-url-segura

Gerar URL encurtada para a autorização.


URLs Encurtadas

POST /api/urls-encurtadas

Criar uma URL encurtada com parâmetros.


{

  "nome": "Bira",

  "telefone": "11999999999",

  "codigoDaUnidade": "R01-QDJ-26",

  "palavraChave": "visitante-2025"

}


Resposta:


{

  "id": "abc12345",

  "urlEncurtada": "/formulario-visitante/abc12345",

  "urlCompleta": "https://cadastro-visitantes.konsilo.online/formulario-visitante/abc12345",

  "criadoEm": "2025-10-28T14:30:00Z",

  "expiracaoEm": "2025-11-04T14:30:00Z"

}


GET /api/urls-encurtadas/{id}

Recuperar dados da URL encurtada.

Resposta:


{

  "nome": "Bira",

  "telefone": "11999999999",

  "codigoDaUnidade": "R01-QDJ-26",

  "palavraChave": "visitante-2025",

  "criadoEm": "2025-10-28T14:30:00Z",

  "expiracaoEm": "2025-11-04T14:30:00Z"

}


DELETE /api/urls-encurtadas/{id}

Deletar uma URL encurtada.


🔐 Segurança

Serialização JSON Segura

A aplicação utiliza uma estratégia customizada de serialização JSON:

✅ Propriedades em lowercase - Todos os nomes de propriedades são convertidos para lowercase


✅ Null ignorados - Propriedades com valor null não são incluídas no JSON


✅ Criptografia de parâmetros - URLs encurtadas codificam parâmetros em um único token

Exemplo de JSON serializado:


{

  "number": "5521993901365",

  "mediatype": "document",

  "mimetype": "application/pdf",

  "caption": "✅ Autorização de Acesso Aprovada",

  "media": "https://seu-dominio.com/api/autorizacoes/...",

  "filename": "comprovante.pdf"

}


Extensão HttpClientExtensions

Método customizado para POST com serialização segura:


var response = await _httpClient.PostAsJsonCamelCaseAsync(url, payload, ct);



⏰ Configuração de TimeZone

A aplicação utiliza TimeZone global configurado para Brasil (São Paulo):


// Program.cs

TimeZoneConfiguration.Inicializar();


Arquivo: Infrastructure/Configuration/TimeZoneConfiguration.cs

Suporta tanto Windows quanto Linux:

Windows: E. South America Standard Time


Linux: America/Sao_Paulo


📊 Status Dinâmico de Autorizações

O status é calculado automaticamente baseado em regras de negócio:

| Regra | Condição | Status |

|-------|----------|--------|

| 1 | Cancelado | Cancelado (permanece) |

| 2 | dataInicio < hoje + sem CheckIn/CheckOut | Expirado |

| 3a | Com CheckIn + CheckOut + dataFim <= hoje | Finalizado |

| 3b | Com CheckIn + CheckOut + dataFim > hoje | Utilizado |

| 4 | Periodo=Unico + dataInicio < hoje + CheckIn aberto | Utilizado |

| 5a | dataInicio <= hoje + sem CheckIn + dataFim < hoje | Expirado |

| 5b | dataInicio <= hoje + sem CheckIn + dataFim >= hoje | Autorizado |

| 6 | dataInicio > hoje | Autorizado |

Implementação:


public StatusAutorizacao Status

{

    get => CalcularStatusAtual();

    set => _status = value;

}



💾 Persistência de Dados

A aplicação utiliza armazenamento em JSON para simplicidade:

Autorizações: Data/autorizacoes.json


URLs Encurtadas: Data/urlsEncurtadas.json


Documentos: Data/documentos.json

Sincronização Thread-Safe

Utiliza SemaphoreSlim para garantir acesso seguro aos arquivos:


private static readonly SemaphoreSlim _mutex = new(1, 1);



🔄 Fluxo de Autorização


1. Criar Autorização

   ↓

   Status = Autorizado (calculado dinamicamente)

   ↓

   Enviar comprovante via WhatsApp



2. Gerar URL Encurtada

   ↓

   Compartilhar link com visitante



3. Visitante acessa URL

   ↓

   Recupera dados do formulário



4. Fazer Check-in

   ↓

   Status = Utilizado



5. Fazer Check-out

   ↓

   Status = Finalizado (se dataFim <= hoje)

   ou Status = Utilizado (se dataFim > hoje)



6. Se passar dataFim sem CheckIn

   ↓

   Status = Expirado



7. Se cancelar

   ↓

   Status = Cancelado (permanece assim)



📝 Configuração (appsettings.json)


{

  "Logging": {

    "LogLevel": {

      "Default": "Information"

    }

  },

  "Mensageria": {

    "BaseUrl": "https://api.whatsapp.com",

    "Instance": "sua-instancia",

    "ApiKey": "sua-chave-api"

  },

  "AllowedHosts": ""

}



🧪 Desenvolvimento Futuro

 [ ] Persistência em PostgreSQL

 [ ] Autenticação JWT

 [ ] Testes unitários com xUnit

 [ ] Middleware para tratamento global de exceções

 [ ] Integração com sistema de câmeras

 [ ] Relatórios de acesso

 [ ] Dashboard administrativo


📄 Licença

Distribuído sob a licença MIT.

Livre para uso, modificação e redistribuição, desde que mantidos os créditos originais.


Autor: Ubirajara Mendes Nunes Junior

Projeto: Gestão de Condomínio - Controle de Portaria API

Versão: 1.0.0

Framework: .NET 8