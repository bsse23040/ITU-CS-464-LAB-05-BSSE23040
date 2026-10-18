# ITU-CS-464-LAB-05-BSSE23040

CS-464 Game Development, Lab 05. I rebuilt the Roll-a-Ball game from Lecture 06 (Forces and Collectibles) and then added the three lab tasks on top of it.

Made in Unity 6000.6.2f1 (URP template) with C#. Roll number: BSSE23040.

## The game

You roll a ball around a small walled arena and collect spinning gold coins. Coins appear one by one (up to 5 at a time). When the score reaches the target, a "YOU WIN!" panel shows up with a Restart button.

**Controls:** `W A S D` or the arrow keys push the ball.

| Playing | Win screen |
|---|---|
| ![playing](Docs/screenshots/1-playing.png) | ![win](Docs/screenshots/4-win.png) |

What comes from the lecture:

- `PlayerController`: reads input in `Update`, then pushes the ball with `AddForce` in `FixedUpdate`.
- `Coin`: a trigger collider. When the ball touches it, it tells the `GameManager` and destroys itself.
- `Spawner`: spawns a coin every 1.5 s at a random spot, and stops at 5 coins (it counts its own children).
- `GameManager`: the only owner of the score. It updates the text, shows the win panel, freezes time and restarts the scene.

## The three tasks

### 1. One Red Coin at a random score between 1 and 10
When the game starts, `GameManager` picks a random number from 1 to 10 (`redCoinScore`). The first time the score reaches that number, the `Spawner` puts one red coin in the arena. A `redCoinSpawned` flag makes sure it can only happen once per game. The red coin is not a child of the spawner, so it does not use up one of the 5 normal coin slots.

![red coin](Docs/screenshots/2-red-coin.png)

### 2. Collecting it spawns 50 Blue Coins around the player
When the red coin is collected, the `Spawner` creates exactly 50 blue coins in a ring around where the ball is (between 1.5 m and 8 m away). They are spread evenly over the area (square root of a random squared distance), they keep a little distance from each other, and they stay inside the walls. If the ball is near a wall, the coins that would fall outside are placed on the arena side instead, so the count is always exactly 50.

### 3. The hidden "BonusCoins: 0/50" text
The text exists in the scene but is switched off. It shows up when the bonus round starts, and every blue coin you collect adds one to it.

![blue coins](Docs/screenshots/3-blue-coins.png)

## Decisions I made

- **Win score is 20, not 10.** The red coin can appear as late as score 10. With a target of 10 the game could be won before you ever get to see the red coin, so I raised the target to 20.
- **The red coin also counts as a normal coin** (+1 score). Blue coins do not change the score, only the BonusCoins counter.
- **When does the text show up?** The task can be read two ways. I show it as soon as the red coin is collected, so you see `BonusCoins: 0/50` right when the 50 blue coins appear. It is a one line change in `GameManager.CollectRedCoin` if it should only appear on the first blue coin.
- The bonus round is part of the same game, so winning and Restart still work. Restart resets the score, the text and all coins.

## Project structure

```
Assets/
  Scripts/     PlayerController, Coin, Spawner, GameManager
  Prefabs/     Coin (gold), RedCoin, BlueCoin
  Materials/   Floor, Wall, Ball and the three coin colours
  Scenes/      Game.unity
  Editor/      Lab05SceneBuilder (builds the scene), Lab05Playtest (automatic test)
Docs/screenshots/   the pictures used in this README
```

## Run it

1. Open the project with Unity 6000.6.2f1.
2. Open `Assets/Scenes/Game.unity` and press Play.

If the scene is ever broken, `Lab05 > Build Game Scene` in the menu bar rebuilds the scene and the prefabs from code.

## Automatic play test

`Lab05 > Run Playtest` opens the scene, enters Play mode and plays by moving the ball onto coins. It checks the score, that exactly one red coin appears at the picked score, that 50 blue coins spawn inside the ring and the walls, that the BonusCoins text is hidden and then shows `0/50`, that blue coins do not change the score, the win, and the Restart. The results are printed in the Console as `[Playtest] ...` and it saves the screenshots above. The last run passed all its checks.

## Git workflow

Work was done on the `development` branch in small commits (one per step: project setup, player, coins, spawner, win and restart, each task, the fixes) and merged into `main` with a pull request.
