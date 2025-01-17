# Leeterview - Backend (.NET Web API)

Leeterview's backend service is developed with **.NET Core Web API**, providing features such as solution sharing, a points system, and user management.

## 📦 Tech Stack
- **Framework**: .NET 7  
- **Database**: MSSQL  
- **Containerization**: Docker  
- **Deployment**: AWS ECS

## 🔧 Environment Requirements
- .NET 7 SDK  
- MSSQL Database  
- Docker (optional)

## 🚀 Installation & Running

1. Restore dependencies:
   ```bash
   dotnet restore

## Start the development server:
    npm run dev

## 🐳 docker execute
    docker build -t leeterview-frontend .
    docker run -p 3000:3000 leeterview-frontend

## 🛠️ Git Hooks - Husky Setup
1. Install Husky and Commitlint:
    npm install husky @commitlint/{config-conventional,cli} --save-dev

2. Enable Husky:
    npx husky install

3. Auto-install Git Hooks:
Add this to package.json:
    "scripts": {
        "prepare": "husky install"
    }

4. Add Commit Message Linting:
    npx husky add .husky/commit-msg "npx --no-install commitlint --edit $1"

5. Create commitlint.config.js:
    module.exports = {
    extends: ['@commitlint/config-conventional'],
    };

## 🏷️ Git Branch Policy

### **`main`**  
- **Production** branch with the latest **stable** and **deployable** version.  
- Only merged from `develop` after testing.

### **`develop`**  
- **Development** branch for integrating **completed features**.  
- Regularly merged into `main` for releases.

### **`feature/*`**  
- For **new feature development**.  
- Created from `develop` and merged back when done.