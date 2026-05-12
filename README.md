# 📘 Study Planner

**Study Planner** is a modern desktop productivity application designed to help students and professionals manage their study time effectively using the **Pomodoro Technique**.

Built with **.NET 8**, **WPF**, and following **Clean Architecture** principles, it offers a sleek dark-mode interface, real-time statistics, and data export capabilities.

---

## 🚀 Features

- 🍅 **Pomodoro Timer**  
  Customizable work (25m), short break (5m), and long break (15m) intervals.

- 📊 **Visual Statistics**  
  Interactive weekly charts showing study hours per day using **LiveCharts2**.

- 📚 **Subject Management**  
  Create, prioritize, and track multiple study subjects.

- ⚙️ **Custom Settings**  
  Adjust timer durations to fit your personal workflow.

- 🌗 **Dark/Light Theme**  
  Seamless theme switching with a modern UI design.

- 📥 **Data Export**  
  Export your study history to CSV for analysis in Excel or Google Sheets.

- 📄 **PDF Reports**  
  Generate professional weekly progress reports.

- ⌨️ **Keyboard Shortcuts**
  - `Space` → Start/Pause timer
  - `Esc` → Stop timer

---

# 🏗️ Architecture & Tech Stack

The project follows **Clean Architecture** to ensure:

- Separation of concerns
- Scalability
- Maintainability
- Testability

---

## 💻 Technology Stack

| Technology | Description |
|---|---|
| **.NET 8** | Main framework |
| **WPF** | Desktop UI Framework |
| **MVVM** | Architecture pattern |
| **CommunityToolkit.Mvvm** | MVVM Toolkit |
| **SQLite** | Local database |
| **Entity Framework Core** | ORM |
| **LiveCharts2 + SkiaSharp** | Charts and statistics |
| **QuestPDF** | PDF generation |
| **CsvHelper** | CSV export |
| **Microsoft.Extensions.DependencyInjection** | Dependency Injection |

---

# 📦 Project Structure

```text
StudyPlanner
│
├── StudyPlanner.Core
├── StudyPlanner.Application
├── StudyPlanner.Data
├── StudyPlanner.Presentation
└── StudyPlanner.Tests
```

---

# ⚡ Installation

## ✅ Prerequisites

Before running the project, make sure you have installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022
  - Include the **.NET Desktop Development** workload

---

## 🔧 Setup

### 1. Clone the repository

```bash
git clone https://github.com/yourusername/StudyPlanner.git
```

---

### 2. Navigate to the project folder

```bash
cd StudyPlanner
```

---

### 3. Restore NuGet packages

```bash
dotnet restore
```

---

### 4. Update Database

The app uses SQLite.

Run the following command:

```bash
dotnet ef database update
```

If you don't have EF tools installed:

```bash
dotnet tool install --global dotnet-ef
```

---

### 5. Run the application

```bash
dotnet run --project StudyPlanner.Presentation
```

---

# 🎮 Usage

## ➕ Add Subjects

Enter:
- Subject name
- Priority (1–10)

Then click **Add**.

---

## 📚 Select Subject

Choose the subject currently being studied from the timer dropdown.

---

## ⏱️ Start Timer

Click **Start** or press:

```text
Space
```

to begin a focus session.

---

## 📈 Track Progress

The circular timer visualizes remaining time.

Completed sessions are automatically recorded.

---

## 📊 View Statistics

Use the **Weekly Progress** chart to monitor study habits.

---

## 📥 Export Data

Click the 📊 icon to export study history to CSV.

---

# ⚙️ Configuration

Customize timer durations in the **Settings** panel.

| Setting | Default |
|---|---|
| Focus Duration | 25 min |
| Short Break | 5 min |
| Long Break | 15 min |

A long break activates after every **4 focus sessions**.

---

# 🧪 Testing

Run unit tests using:

```bash
dotnet test
```

---

# 🤝 Contributing

Contributions are welcome!

## Steps

1. Fork the repository
2. Create a feature branch

```bash
git checkout -b feature/AmazingFeature
```

3. Commit your changes

```bash
git commit -m "Add some AmazingFeature"
```

4. Push to the branch

```bash
git push origin feature/AmazingFeature
```

5. Open a Pull Request

---

# 📝 License

Distributed under the **MIT License**.

See the `LICENSE` file for more information.

---

# 👤 Author

## Ernelson Barros

- 📧 Email: ernelson.fdebarros@gmail.com
- 🐙 GitHub:(https://github.com/Ernelson123)

---

# ⭐ Support

If you like this project, consider giving it a ⭐ on GitHub!
