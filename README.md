# eShop with OpenTelemetry Integration

This project integrates OpenTelemetry into the eShop microservices environment, enabling observability with **tracing and metrics collection**. The system exports metrics and traces to **OTel Collectot** then to **Prometheus** or **Jaeger**, and provides **Grafana dashboards** for visualization.

---

## **1. Building and Running the eShop Environment**

### **Prerequisites**
Ensure you have the following installed:
- **Docker** & **Docker Compose**

### **Build & Start Services**
1. **Clone the repository:**
  ```sh
  git clone https://github.com/RobertoCastro391/eShop_AS_02.git
  cd eShop_AS_02
  ```
2. **Open `eShop.sln` project:**
  Click on Start Project to run it

3. **Start OTel Collector, Prometheus, Grafana and Jaeger Services:**
  Make sure you are on the project root folder `/eShop_AS_02`
  ```sh
  docker-compose up -d --build
  ```
4. **Verify services:**

- Aspire Dashboard: https://localhost:19888
- WebApp: https://localhost:7928
- Prometheus: http://localhost:9090
- Jaeger UI: http://localhost:16686
- Grafana UI: http://localhost:3000

### **Setting Up & Viewing the Grafana Dashboard**

Grafana is pre-configured with three dashboards to display OpenTelemetry metrics and traces.

#### **Access Grafana:**

1. Open your browser and go to: http://localhost:3000.  
2. Login using default credentials: (admin, admin).
3. Go to **Dashboard** and select the dashbard you want.
