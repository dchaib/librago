import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import { CSPProvider } from "@base-ui/react/csp-provider"

import "./index.css"
import App from "./App.tsx"

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <CSPProvider disableStyleElements>
      <App />
    </CSPProvider>
  </StrictMode>
)
