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

### **1️. Install Husky and Commitlint**

```bash
npm install husky @commitlint/{config-conventional,cli} --save-dev
```

### **2️. Enable Husky**

```bash
npx husky install
```

### **3️. Auto-install Git Hooks**

Add this to **`package.json`**:

```json
"scripts": {
  "prepare": "husky install"
}
```

### **4️. Add Commit Message Linting**

```bash
npx husky add .husky/commit-msg "npx --no-install commitlint --edit $1"
```

### **5️. Create `commitlint.config.js`**

```javascript
module.exports = {
  extends: ['@commitlint/config-conventional'],
};
```

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