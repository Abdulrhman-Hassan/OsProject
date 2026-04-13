# 🧠 CPU Scheduler Simulator (OS Project 2026)

A full-featured **CPU Scheduling Simulator** developed as part of the Operating Systems course. This project provides a visual and interactive way to understand how different scheduling algorithms manage process execution.

---

## 📌 Project Description

This application simulates multiple CPU scheduling algorithms with a **live execution environment**, allowing users to:

- Add processes dynamically while the scheduler is running  
- Visualize execution using a **live Gantt Chart**  
- Track process states through a **remaining burst time table**  
- Compare performance using:
  - Average Waiting Time  
  - Average Turnaround Time  

The simulator is implemented as a **GUI desktop application**, where each time unit equals **1 second**.

---

## 🚀 Supported Scheduling Algorithms

The system supports the following CPU scheduling algorithms:

- 🔹 First Come First Serve (FCFS)  
- 🔹 Shortest Job First (SJF)
  - Preemptive  
  - Non-Preemptive  
- 🔹 Priority Scheduling  
  - Preemptive  
  - Non-Preemptive  
- 🔹 Round Robin (RR)  

> ⚠️ Note: Lower priority number means higher priority.

---

## ⚙️ Features

- ✅ Interactive GUI Desktop Application  
- ✅ Live Scheduling Simulation (Real-Time Execution)  
- ✅ Dynamic Process Addition أثناء التشغيل  
- ✅ Live Gantt Chart Visualization  
- ✅ Remaining Burst Time Table (Updated Every Second)  
- ✅ Option to Run Static Scheduling (without live updates)  
- ✅ Smart Input Handling (only required fields per algorithm)  

---

## 🧾 Inputs

The simulator accepts:

- Scheduling Algorithm Type  
- Number of Processes  
- Process Data (depends on selected algorithm), such as:
  - Arrival Time  
  - Burst Time  
  - Priority (if required)  
  - Time Quantum (for Round Robin)  

> The system only asks for relevant inputs based on the selected algorithm.

---

## 📊 Outputs

- 📈 Live Gantt Chart (execution timeline)  
- ⏱ Average Waiting Time  
- 🔄 Average Turnaround Time  
- 📋 Remaining Burst Time Table (live updates)  

---

## 🖥️ System Requirements

- Windows OS  
- .NET Runtime (if required)  
- Executable file included  

---

## 📦 Deliverables

- Source Code  
- Executable (.exe) File  
- Project Report (PDF with screenshots & team info)  
- GitHub Repository  

---

## 👨‍💻 Contributors

| Name                | ID       | Responsibility                |
|---------------------|----------|-------------------------------|
| Saeed Bayoumy       | 2200950  | FCFS Algorithm               |
| Zeyad Abdallah      | 2200932  | Round Robin Algorithm        |
| Marcelino Maged     | 2200909  | Priority Preemptive          |
| Mohamed Ali         |          | SJF Non-Preemptive           |
| Abdelrahman Hassan  |          | SJF Preemptive               |
| Omar Mohsen         |          | Priority Non-Preemptive      |

---

## 🎯 Project Objective

The goal of this project is to provide a practical understanding of CPU scheduling concepts by:

- Demonstrating how different algorithms behave  
- Comparing their efficiency  
- Visualizing process execution in real time  

---

## 💡 Future Improvements

- Add more advanced scheduling algorithms  
- Export results as reports  
- Improve UI/UX  
- Add performance comparison charts  

---

## 📎 Repository Link

👉 https://github.com/Abdulrhman-Hassan/OsProject

---

✨ *Built with teamwork, logic, and a lot of debugging 😄*
