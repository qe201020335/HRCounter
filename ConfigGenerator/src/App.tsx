import './App.css';
import GeneratorMain from "./Components/Main";
import {Link} from "@mui/material";

function App() {

  return (
      <div className="App" >
        <h1>HRCounter Easy Config Generator</h1>
        <h3>DO NOT USE THIS IF YOU ALREADY HAVE DATA SOURCE CONFIGURED</h3>
        
        <GeneratorMain/>
        
        <div id="credits">
            There isn't a lot going on on this page. <Link href="https://github.com/qe201020335" target="_blank" underline="hover">@qe201020335</Link>
        </div>
      </div>
  )
}

export default App;
