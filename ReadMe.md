### <img src="_Res/Logo.png" alt="Logo" style="zoom:15%;" />   Battle Soup

------

> AI for [BattleTabs](https://battletabs.io)







##### Contact

- BattleTabs 🎃+🥒=🥘  id:LclSU0kfN

- Twitter [@_Moenen](https://twitter.com/_Moenen)
- Email moenen6@gmail.com



##### Requirement

- Unity Editor 2021.1.13f1c1
- API Compatibility Level .NET 4.x
- Decompress the zip file before use



##### Update

`v0.0.1`

-  `Player vs AI` and `AI vs AI` mode. 
- Select ships without limitation. It can be more than 4 ships or less than 4 ships. Same ship can be select multiple times.

- In "(game folder)/Ships", you can find the folders contains ships data. The name of a ship's folder is the `Ship ID`. In the folder, is a json file contains all the information of the ship, and a png image for the ship icon. 
- In "(game folder)/Maps", the maps are saved as png file only contains black and white pixels. Width and height of the image must be same.
- The AI is coded as `Strategy` and it's modular. Each strategy is a C# file inherited from SoupStrategy.cs. 
- Strategy have their own Display name, Description and Fleet. The fleet is saved as a ship id array such as "Coracle", "Whale", "KillerSquid", "Turtle".










