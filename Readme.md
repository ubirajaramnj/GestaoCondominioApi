# 🏢 API de Consulta de Unidades e Condôminos

Este projeto é uma **API REST desenvolvida em .NET 8**, responsável por fornecer informações sobre **unidades de condomínio** e seus respectivos **condôminos**, a partir do **código do condomínio** e do **telefone** informado.

A aplicação utiliza **Swagger** para documentação e segue uma estrutura limpa e escalável, permitindo fácil expansão futura para integração com banco de dados ou outros serviços.

---

## 🐳 Execução via Docker

O projeto inclui um **Dockerfile otimizado para .NET 8**, permitindo build e execução tanto em modo de desenvolvimento quanto de produção.

### 📁 Estrutura de Build em Camadas

O `Dockerfile` segue uma arquitetura **multi-stage build**, com quatro estágios:

| Estágio   | Base                                  | Função                                           |
| --------- | ------------------------------------- | ------------------------------------------------ |
| `base`    | `mcr.microsoft.com/dotnet/aspnet:8.0` | Ambiente leve de execução (runtime).             |
| `build`   | `mcr.microsoft.com/dotnet/sdk:8.0`    | Compila e restaura dependências.                 |
| `publish` | `mcr.microsoft.com/dotnet/sdk:8.0`    | Publica o artefato final otimizado.              |
| `final`   | `mcr.microsoft.com/dotnet/aspnet:8.0` | Imagem final com o binário pronto para produção. |

---

### ⚙️ **1️⃣ Construindo a imagem**

No diretório raiz do projeto, execute:

```bash
docker build -t gestao-condominio-api -f GestaoCondominio.ControlePortaria.Api/Dockerfile .
```

🔹 Explicação:

* O parâmetro `-f` define o caminho para o Dockerfile.
* A flag `-t` atribui um nome à imagem (`gestao-condominio-api`).

---

### 🧩 **2️⃣ Executando o container**

Execute o container com o comando:

```bash
docker run -d -p 8090:8090 -p 8091:8091 --name condominio-api gestao-condominio-api
```

🔹 Isso fará com que:

* A API rode em **duas portas expostas**:

  * `8080` — acesso HTTP padrão
  * `8081` — porta adicional usada pelo Visual Studio / ambiente de debug
* O container rode em **modo daemon** (`-d`), permitindo execução em background.

---

### 🧭 **3️⃣ Acessando a API**

Após o container estar em execução, acesse:

* Swagger UI:
  👉 [http://localhost:8080/swagger](http://localhost:8080/swagger)

* Endpoint direto:
  👉 `http://localhost:8080/Condominio/1/Telefone/5521993901365`

---

### 🧰 **4️⃣ Variáveis e Personalização**

Você pode personalizar o comportamento do build com argumentos (`--build-arg`) e variáveis de ambiente.

Exemplo de build com modo de compilação específico:

```bash
docker build -t gestao-condominio-api --build-arg BUILD_CONFIGURATION=Debug .
```

---

### 🧹 **5️⃣ Limpando containers e imagens**

Para remover containers parados e imagens antigas:

```bash
docker ps -a
docker stop condominio-api
docker rm condominio-api
docker rmi gestao-condominio-api
```

---

## 🧠 Estrutura do Projeto

```
GestaoCondominio.ControlePortaria.Api/
├── Controllers/
│   └── UnidadesController.cs
├── Model/
│   ├── CondominioApi/
│   │   └── Models/
│   │       ├── Unidade.cs
│   │       └── Condomino.cs
├── Services/
│   └── UnidadeService.cs
├── Program.cs
└── Dockerfile
```

---

## 📡 Endpoint Disponível

### **GET** `/Condominio/{codCondominio}/Telefone/{telefone}`

Retorna informações da unidade e dos condôminos associados ao telefone informado.

#### Exemplo:

```
GET /Condominio/1/Telefone/5521993901365
```

#### Resposta:

```json
{
  "codigoCondominio": 1,
  "nomeDoCondominio": "Condominio Solar de Itacuruça",
  "codigoDaUnidade": "1L26J",
  "rua": "Rua 1",
  "numeroDoLote": 26,
  "codigoDaQuadra": "J",
  "condominos": [
    {
      "nome": "Ubirajara Mendes Nunes Junior",
      "telefone": 5521993901365
    }
  ]
}
```

---

## 📘 Estrutura e Roteamento

A rota é configurada no controlador como:

```csharp
[HttpGet()]
[Route("/Condominio/{codCondominio}/Telefone/{telefone}")]
```

Endpoint acessível diretamente via raiz da API (sem prefixo `/api`).

---

## 🧩 Desenvolvimento Futuro

* [ ] Persistência real em **PostgreSQL**
* [ ] Autenticação JWT
* [ ] Testes unitários com **xUnit**
* [ ] Middleware para tratamento global de exceções

---

## 📄 Licença

Distribuído sob a **licença MIT**.
Livre para uso, modificação e redistribuição, desde que mantidos os créditos originais.

---

**Autor:** Ubirajara Mendes Nunes Junior
**Projeto:** Gestão de Condomínio - Controle de Portaria API
