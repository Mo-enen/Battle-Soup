# <img src="_Res/Logo Small.png" alt="Logo" style="zoom:100%;" />    Battle Soup

> Fan-made game for [BattleTabs](https://battletabs.io)

Download latest  version at [Itch](https://m-oenen.itch.io/battlesoup) or [Github](https://github.com/Mo-enen/Battle-Soup/releases)

<img src="_Res\0.jpg">

<img src="_Res\1.jpg">

<img src="_Res\2.jpg">

<img src="_Res\3.jpg">

<img src="_Res\4.jpg">

<img src="_Res\5.jpg">



##### Contact

- BattleTabs 🎃+🥒=🥘  id:LclSU0kfN
- Twitter [@_Moenen](https://twitter.com/_Moenen)
- Email moenen6@gmail.com





##### Develop Requirement

- Unity Editor 2022.2.0b1
- API Compatibility Level .NET 4.x.
- Decompress the zip file before use.
- Available on PC only.




##### Change Log

`v2.1.0`

- New feature: Ship Editor. Create custom ships with your own shape and ability.

`v2.0.0`

- Completely remake, new artwork, new UI, new AI.
- Custom code for ship ability, players can create their own ability by using the code.
- Using AngeliA Framework (a 2D Unity-based framework made by Moenen).

`v1.1.0`

- Ship editor available now. Create your own ship with custom body shape, ability and icon image.

- Minor changes on ability logic.


`v1.0.0`

-  `Player vs AI` and `AI vs AI` mode. 
- Select ships without limitation. It can be more than 4 ships or less than 4 ships. Same ship can be select multiple times.

- In "(game folder)/Ships", you can find the folders contains ships data. The name of a ship's folder is the `Ship ID`. In the folder, is a json file contains all the information of the ship, and a png image for the ship icon. 
- In "(game folder)/Maps", the maps are saved as png file only contains black and white pixels. Width and height of the image must be same.
- The AI is coded as `Strategy` and it's modular. Each strategy is a C# file inherited from SoupStrategy.cs. 
- Strategy have their own Display name, Description and Fleet. The fleet is saved as a ship id array such as "Coracle", "Whale", "KillerSquid", "Turtle".











