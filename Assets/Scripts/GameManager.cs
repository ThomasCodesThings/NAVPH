using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameStructs;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using AI;
using System;

public class GameManager : MonoBehaviour
{

    /************************************************************************
     * 
     *  Prefabs and Variables
     * 
     * *********************************************************************/

    [SerializeField] GameObject dirtPrefab;
    [SerializeField] GameObject floorPrefab;
    [SerializeField] GameObject floorLeftPrefab;
    [SerializeField] GameObject floorRightPrefab;
    [SerializeField] GameObject platformCenterPrefab;
    [SerializeField] GameObject platformLeftPrefab;
    [SerializeField] GameObject platformRightPrefab;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] int width = 100;
    [SerializeField] int height = 25;
    [SerializeField] GameObject player;
    [SerializeField] GameObject soldierPrefab;
    [SerializeField] GameObject dronePrefab;
    [SerializeField] GameObject bossPrefab;
    [SerializeField] GameObject medkitPrefab;
    [SerializeField] GameObject ammoPackPrefab;
    [SerializeField] GameObject lampPrefab;

    /************************************************************************
     * 
     *  Texts
     * 
     * *********************************************************************/

    [SerializeField] TMP_Text healthText;
    [SerializeField] Slider healthBar;
    [SerializeField] Slider xpBar;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text waveText;
    [SerializeField] TMP_Text timerText;
    [SerializeField] TMP_Text ammoText;
    [SerializeField] TMP_Text magazineText;
    [SerializeField] TMP_Text weaponNameText;
    [SerializeField] TMP_Text medkitsCountText;
    /************************************************************************
     * 
     *  Private Variables
     * 
     * *********************************************************************/

    private GameObject difficultyManager;
    private PlayerStats playerStats;
    private float totalElapsedTime = 0.0f;
    private float gracePeriod = 5.0f;
    private float duration = 1.0f;
    private int currentWave = 1;
    private int soldierCount = 1;
    private int droneCount = 1;
    private int medkitCount = 1;
    private int ammoCount = 1;
    private int maxWaves = 10;
    private int platformLayers = 2;


    private List<int> heights = new List<int>();
    private List<Level> levels = new List<Level>();
    private Difficulty currentDifficulty = Difficulty.Easy;
    private string[] medkitTypes = { "small", "medium", "large" };
    private string[] ammoTypes = { "small", "medium", "large" };
    private Dictionary<int, GameObject> weaponThresholds;
    private HashSet<int> spawnedThresholds;

    private List<GameObject> enemies = new List<GameObject>();
    private List<GameObject> medkits = new List<GameObject>();
    private List<GameObject> ammos = new List<GameObject>();
    private string currentWeapon = "Basic Pistol";
    private int[,] blockGrid;
    private AStar AStarSearch;

    private int seed;

    System.Random random;

    /************************************************************************
     * 
     *  Weapons
     * 
     * *********************************************************************/

    [SerializeField] GameObject smgPrefab;
    [SerializeField] GameObject shotgunPrefab;
    [SerializeField] GameObject laserPrefab;
    [SerializeField] GameObject plasmaCannonPrefab;

    [SerializeField] GameObject baseBulletPrefab;
    [SerializeField] GameObject invisBulletPrefab;
    [SerializeField] GameObject plasmaBulletPrefab;

    [SerializeField] Texture2D basePistolIcon;
    [SerializeField] Texture2D smgIcon;
    [SerializeField] Texture2D shotgunIcon;
    [SerializeField] Texture2D laserIcon;
    [SerializeField] Texture2D plasmaCannonIcon;

    private Dictionary<string, RangeWeapon> weapons = new Dictionary<string, RangeWeapon>();

    private List<(int, int)> blockedCells = new List<(int, int)>();

    private GameObject audioManager;

    /************************************************************************
     * 
     *  Stats
     * 
     * *********************************************************************/

    private int playerXp = 0;
    private int playerMedkitsUsed = 0;
    private int playerEnemiesKilled = 0;




    public List<int> getHeights(){
        return heights;
    }

    public int getBaseWidth(){
        return width;
    }

    public float getElapsedTime(){
        return totalElapsedTime;
    }

    public void setCurrentWeapon(string weapon){
        currentWeapon = weapon;
    }

    public RangeWeapon getCurrentWeapon(){
        return weapons[currentWeapon];
    }

    public void addAmmo(int amount){
        weapons[currentWeapon].addAmmo(amount);
    }

    public void addKill(){
        playerEnemiesKilled++;
    }

    public void addMedkitsUsed(){
        playerMedkitsUsed++;
    }

    public void addXP(int xp){
        playerXp += xp;
    }

    public int getPlayerXP(){
        return playerXp;
    }

    public int getPlayerMedkitsUsed(){
        return playerMedkitsUsed;
    }

    public int getPlayerEnemiesKilled(){
        return playerEnemiesKilled;
    }


    // Generate platform, with left side, center and left side prefabs, on both ends add lamps
    private void generatePlatform(int startX, int startY, int platformWidth)
    {
        int prefabLength = 2;

        // Ensure the platform does not exceed grid bounds
        if (startX + platformWidth > width)
        {
            Debug.LogWarning("Platform cannot be placed due to out-of-bounds coordinates.");
            return;
        }

        // Instantiate the left prefab
        if (isWithinBlockGrid(startX + width, startY) && isWithinBlockGrid(startX + width + 1, startY))
        {
            Instantiate(platformLeftPrefab, new Vector3(startX, startY, 0), Quaternion.identity);
            blockGrid[startX + width, startY] = 0;
            blockGrid[startX + width + 1, startY] = 0;
            blockedCells.Add((startX, startY));
            blockedCells.Add((startX + 1, startY));

            if(!blockedCells.Contains((startX, startY + 3))){
                Instantiate(lampPrefab, new Vector3(startX, startY + 3, -5), Quaternion.identity);
            }
        }

        // Instantiate center prefabs
        for (int x = startX + prefabLength; x < startX + platformWidth - prefabLength; x += prefabLength)
        {
            if (isWithinBlockGrid(x + width, startY) && isWithinBlockGrid(x + width + 1, startY))
            {
                Instantiate(platformCenterPrefab, new Vector3(x, startY, 0), Quaternion.identity);
                blockGrid[x + width, startY] = 0;
                blockGrid[x + width + 1, startY] = 0;
                blockedCells.Add((x, startY));
                blockedCells.Add((x + 1, startY));
            }
        }


        int rightX = startX + platformWidth - prefabLength;
        if (isWithinBlockGrid(rightX + width, startY) && isWithinBlockGrid(rightX + width + 1, startY))
        {
            Instantiate(platformRightPrefab, new Vector3(rightX, startY, 0), Quaternion.identity);
            blockGrid[rightX + width, startY] = 0;
            blockGrid[rightX + width + 1, startY] = 0;
            blockedCells.Add((rightX, startY));
            blockedCells.Add((rightX + 1, startY));

            if(!blockedCells.Contains((rightX, startY + 3))){
                Instantiate(lampPrefab, new Vector3(rightX, startY + 3, -5), Quaternion.identity);
            }
        }
    }


    public int generateFloorPlatform(int startX, int startY, int platformWidth)
    {
        int totalWidth = 0;
        bool ignoreLeftFloor = startX == -width + 1; // Ignore left floor if the platform starts at the left edge
        bool ignoreRightFloor = startX + platformWidth > width - 1; // Ignore right floor if the platform ends at the right edge

        if (!ignoreLeftFloor) // If the platform does not start at the left edge, instantiate the left floor prefab
        {
            if (isWithinBlockGrid(startX + width, startY))
            {
                Instantiate(floorLeftPrefab, new Vector3(startX, startY, 0), Quaternion.identity);
                blockGrid[startX + width, startY] = 0;
                heights.Add(startY);
                blockedCells.Add((startX, startY));
                totalWidth++;
            }
        }

        int calculatedWidth = platformWidth;
        if (startX + platformWidth > width) // Calculate proper floor width
        {
            calculatedWidth = width - startX;
        }

        // Instantiate center floor prefabs
        for (int x = (ignoreLeftFloor ? startX : startX + 1); x < startX + calculatedWidth - 1; x++)
        {
            if (isWithinBlockGrid(x + width, startY))
            {
                Instantiate(floorPrefab, new Vector3(x, startY, 0), Quaternion.identity);
                heights.Add(startY);
                blockGrid[x + width, startY] = 0;
                blockedCells.Add((x, startY));
                totalWidth++;
            }
        }

        if (!ignoreRightFloor) // If the platform does not end at the right edge, instantiate the right floor prefab
        {
            if (isWithinBlockGrid(startX + calculatedWidth - 1 + width, startY))
            {
                Instantiate(floorRightPrefab, new Vector3(startX + calculatedWidth - 1, startY, 0), Quaternion.identity);
                heights.Add(startY);
                blockGrid[startX + calculatedWidth - 1 + width, startY] = 0;
                blockedCells.Add((startX + calculatedWidth - 1, startY));
                totalWidth++;
            }
        }

        for (int y = startY - 1; y >= -15; y--) // Fill the space below the platform with dirt
        {
            for (int x = startX; x < startX + totalWidth; x++)
            {
                Instantiate(dirtPrefab, new Vector3(x, y, 0), Quaternion.identity);
            }
        }

        return totalWidth;
    }

    private bool isWithinBlockGrid(int x, int y) // Check if the coordinates are within the block grid bounds
    {
        return x >= 0 && x < blockGrid.GetLength(0) && y >= 0 && y < blockGrid.GetLength(1);
    }


    private int revFreeBlock(int x){ // Get the first free block (that is not above void) from the right
        for(int i = x; i > 0; i--){
            if(heights[i] != -1){
                return heights[i];
            }
        }
        return heights[x];
    }

    // Generate drone and boss spawn point
    private Vector3 generateFlyingEnemySpawnPoint(){
        int x = random.Next(-width + 1, width - 1);
        int y = random.Next(height - 3, height - 1);
        return new Vector3(x, y, 0);
    }


    // Function to procedurally generate terrain
   public void generate()
    {
        int wallSize = height;
        //0 - cell is blocked
        //1 -cell is non blocked
        int prevY = 2;

        // Generate floor with random gaps
        for (int x = -width; x < width;)
        {
            bool generateGap = random.Next(0, 4) == 1; // 25% chance to generate a gap
            if(generateGap){
                int gapSize = random.Next(2, 4);
                x += gapSize;
                prevY = random.Next(0, 2) == 0 ? prevY - 1 : prevY + 1;
                for(int k = 0; k < gapSize; k++){
                    heights.Add(-1);
                }
                continue;
            }
            int platformWidth = random.Next(4, 8);
            int platformHeight = random.Next(-2, 3);
            int platformY = prevY + platformHeight;
            if(platformY == prevY){
                platformY = random.Next(0, 2) == 0 ? prevY - 1 : prevY + 1;
            }
            int realWidth = generateFloorPlatform(x, platformY, platformWidth);
            prevY = platformY;
            x += realWidth;
        }


        // Generate walls on the left and right edges
        for (int i = 0; i < height; i++)
        {
            Instantiate(dirtPrefab, new Vector3(-width, i, 0), Quaternion.identity);
            Instantiate(dirtPrefab, new Vector3(width - 1, i, 0), Quaternion.identity);

            blockGrid[0, i] = 0;              
            blockGrid[width * 2 - 1, i] = 0;
        }


        // Generate platforms, go from bottom to top, leave 6 blocks between platforms vertically and 2-4 blocks between platforms horizontally
        int previousY = -1;
        bool previousWasGap = false;

        for (int yMultiplier = 1; yMultiplier < platformLayers + 1; yMultiplier++)
        {
            for (int x = -width; x < width;)
            {
                int randomInt = random.Next(0, 5);
                bool spawnPlatform = randomInt == 1 || randomInt == 2;

                if (spawnPlatform && !previousWasGap)
                {
                    int platformWidth = random.Next(4, 8);
                    if (platformWidth % 2 != 0) // Ensure the platform width is even
                    {
                        platformWidth++;
                    }

                    int y = revFreeBlock(x + width);
                    if (y < 0 || y >= blockGrid.GetLength(1))
                    {
                        continue;
                    }

                    if (y == previousY)
                    {
                        int gapSize = random.Next(2, 4);
                        x += gapSize;
                        previousWasGap = true;
                        continue;
                    }

                    if (x + platformWidth <= width)
                    {
                        generatePlatform(x, y + ((int)6f * yMultiplier), platformWidth);
                        previousY = y;
                        previousWasGap = false;
                    }
                    x += platformWidth;
                }
                else
                {
                    int gapSize = random.Next(2, 4);
                    x += gapSize;

                    if (previousWasGap)
                    {
                        spawnPlatform = true;
                        previousWasGap = false;
                    }
                    else
                    {
                        previousWasGap = true;
                    }

                    if (x >= width)
                    {
                        break;
                    }
                }
            }
        }

    }

    //(number of soldiers, number of drones, number of medkits, number of ammos)
    public (int, int, int, int) getLevelSettings()
{
    switch (currentDifficulty)
    {
        case Difficulty.Easy:
            switch (currentWave)
            {
                case 1: return (1, 0, 1, 2);
                case 2: return (2, 0, 2, 3);
                case 3: return (3, 1, 2, 4);
                case 4: return (5, 1, 3, 5);
                case 5: return (7, 1, 3, 6);
                case 6: return (8, 1, 3, 6);
                case 7: return (10, 2, 3, 7);
                case 8: return (12, 2, 4, 8);
                case 9: return (15, 2, 4, 9);
                case 10: return (18, 2, 5, 10);
                default: return (1, 1, 1, 1);
            }

        case Difficulty.Medium:
            switch (currentWave)
            {
                case 1: return (3, 0, 1, 2);
                case 2: return (5, 1, 1, 3);
                case 3: return (7, 1, 2, 4);
                case 4: return (10, 1, 2, 5);
                case 5: return (12, 2, 3, 6);
                case 6: return (15, 2, 3, 7);
                case 7: return (18, 2, 4, 8);
                case 8: return (20, 3, 4, 9);
                case 9: return (23, 3, 5, 10);
                case 10: return (25, 3, 5, 11);
                default: return (1, 1, 1, 1);
            }

        case Difficulty.Hard:
            switch (currentWave)
            {
                case 1: return (5, 1, 1, 2);
                case 2: return (7, 1, 1, 3);
                case 3: return (10, 1, 2, 4);
                case 4: return (15, 2, 2, 5);
                case 5: return (18, 2, 3, 6);
                case 6: return (20, 3, 3, 7);
                case 7: return (23, 3, 4, 8);
                case 8: return (25, 3, 4, 9);
                case 9: return (28, 4, 5, 10);
                case 10: return (30, 5, 5, 12);
                default: return (1, 1, 1, 1);
            }

        default:
            return (1, 1, 1, 1);
    }
}

    // Calculate the time for each wave based on the current difficulty
    private int calculateWaveTime(){
        switch(currentDifficulty){
            case Difficulty.Easy:
                return (currentWave - 1)  * 60 + 240;
            case Difficulty.Medium:
                return (currentWave - 1)  * 60 + 180;
            case Difficulty.Hard:
                return (currentWave - 1)  * 60 + 120;
            default:
                return 60;
        }
    }

    // Format function
    public string floatToMinutesSeconds(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);

        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Format function
    public string floatToDate(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int hours = Mathf.FloorToInt(minutes / 60);

        return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    // Format function
    public string formatNumber(int number)
    {
        return string.Format("{0:n0}", number);
    }

    // Get the current game stats
    public GameStats getGameStats()
    {
        string formatedTime = floatToDate(totalElapsedTime);
        string formatedEnemiesKilled = formatNumber(playerEnemiesKilled);
        string formatedMedkitsUsed = formatNumber(playerMedkitsUsed);
        string formatedXp = formatNumber(playerXp);
        return new GameStats(formatedTime, formatedEnemiesKilled, formatedMedkitsUsed, formatedXp);
    }

    // Remove collision between enemies and medkits and ammos
    private void removeCollision(List<GameObject> enemies)
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }
            foreach (GameObject medkit in medkits)
            {
                if (medkit == null)
                {
                    continue;
                }
                Physics2D.IgnoreCollision(enemy.GetComponent<Collider2D>(), medkit.GetComponent<Collider2D>());
            }

            foreach (GameObject ammo in ammos)
            {
                if (ammo == null)
                {
                    continue;
                }
                Physics2D.IgnoreCollision(enemy.GetComponent<Collider2D>(), ammo.GetComponent<Collider2D>());
            }
        }

    }

    // Clear all enemies, medkits and ammos after each wave
    public void clearAfterWave(){
        foreach(GameObject enemy in enemies){
            if(enemy != null){
                Destroy(enemy);
            }
        }

        foreach(GameObject medkit in medkits){
            if(medkit != null){
                Destroy(medkit);
            }
        }

        foreach(GameObject ammo in ammos){
            if(ammo != null){
                Destroy(ammo);
            }
        }
    }

    // Generate random spawn point for enemies and items
    private Vector3 generateRandomSpawnPoint(int offsetY = 2)
    {
        int randomIndex = random.Next(0, blockedCells.Count);
        (int x, int y) = blockedCells[randomIndex];
        return new Vector3(x, y + offsetY, 0);
    }

    // Spawn soldiers
    private List<GameObject> spawnSoldiers(int count)
    {
        List<GameObject> enemies = new List<GameObject>(); 
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPoint = generateRandomSpawnPoint();
            GameObject enemy = Instantiate(soldierPrefab, spawnPoint, Quaternion.identity);
            enemies.Add(enemy); 
        }
        return enemies;
    }

    // Spawn drones
    private List<GameObject> spawnDrones(int count)
    {
        List<GameObject> enemies = new List<GameObject>(); 
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPoint = generateFlyingEnemySpawnPoint();
            GameObject enemy = Instantiate(dronePrefab, spawnPoint, Quaternion.identity);
            enemies.Add(enemy); 
        }
        return enemies;
    }

    // Spawn medkits
    private List<GameObject> spawnMedKits(int count){
        List<GameObject> medkits = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPoint = generateRandomSpawnPoint();
            GameObject medkit = Instantiate(medkitPrefab, spawnPoint, Quaternion.identity);
            string medkitType = medkitTypes[UnityEngine.Random.Range(0, medkitTypes.Length)];
            medkit.GetComponent<MedkitScript>().updateMedkit(medkitType);
            medkits.Add(medkit);
        }
        return medkits;
    }

    // Spawn ammo packs
    private List<GameObject> spawnAmmo(int count){
        List<GameObject> ammos = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPoint = generateRandomSpawnPoint();
            GameObject ammo = Instantiate(ammoPackPrefab, spawnPoint, Quaternion.identity);
            string ammoType = ammoTypes[UnityEngine.Random.Range(0, ammoTypes.Length)];
            ammo.GetComponent<AmmoPackScript>().updateAmmoPack(ammoType);
            ammos.Add(ammo);
        }
        return ammos;
    }

    // Clear all decals after each wave
    public void clearDecals(){
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach(GameObject bullet in bullets){
            Destroy(bullet);
        }

        GameObject[] energyProjectiles = GameObject.FindGameObjectsWithTag("EnergyProjectile");
        foreach(GameObject energyProjectile in energyProjectiles){
            Destroy(energyProjectile);
        }

        GameObject[] ballisticProjectiles = GameObject.FindGameObjectsWithTag("BallisticProjectile");
        foreach(GameObject ballisticProjectile in ballisticProjectiles){
            Destroy(ballisticProjectile);
        }
    }

    // Corutine to handle wave
    private IEnumerator handleWaves(){
        while (currentWave <= maxWaves)
        {
            if (waveText == null || timerText == null)
            {
                yield break;
            }
            (int soldiersToSpawn, int dronesToSpawn, int medkitsToSpawn, int ammosToSpawn) = getLevelSettings();
            soldierCount = soldiersToSpawn;
            droneCount = dronesToSpawn;
            medkitCount = medkitsToSpawn;
            ammoCount = ammosToSpawn;
            waveText.text = "GRACE PERIOD";
            timerText.color = Color.red;
            yield return handleGraceTime();
            waveText.text = "WAVE " + currentWave;
            timerText.color = Color.white;
            yield return handleWaveTime();
            currentWave++;
            clearAfterWave();
            clearDecals();
        }
        SceneManager.LoadScene("VictoryScene");
    }

    // Corutine to handle grace period
    private IEnumerator handleGraceTime(){
        duration = gracePeriod;
        medkits = spawnMedKits(medkitCount);
        ammos = spawnAmmo(ammoCount);
        audioManager.GetComponent<AudioScript>().musicSource.Stop();
        audioManager.GetComponent<AudioScript>().musicSource.clip = audioManager.GetComponent<AudioScript>().graceTimeCountdown;
        audioManager.GetComponent<AudioScript>().musicSource.loop = false;
        audioManager.GetComponent<AudioScript>().musicSource.Play();
        while (duration > 0)
        {
            if(timerText == null){
                yield break;
            }
            timerText.text = floatToMinutesSeconds(duration);
            yield return null;
            duration -= Time.deltaTime;
        }
    }

    // Corutine to handle what happening during the wave
    private IEnumerator handleWaveTime()
    {
        duration = calculateWaveTime();
        List<GameObject> soldiers = spawnSoldiers(soldierCount);
        List<GameObject> drones = spawnDrones(droneCount);

        List<GameObject> enemies = new List<GameObject>();
        enemies.AddRange(soldiers);
        enemies.AddRange(drones);
        
        if(currentWave == maxWaves){ // Spawn boss on the last wave
            GameObject boss = Instantiate(bossPrefab, generateFlyingEnemySpawnPoint(), Quaternion.identity);
            enemies.Add(boss);
        }

        removeCollision(enemies);
        audioManager.GetComponent<AudioScript>().musicSource.Stop();
        audioManager.GetComponent<AudioScript>().musicSource.clip = audioManager.GetComponent<AudioScript>().backgroundMusic;
        audioManager.GetComponent<AudioScript>().musicSource.loop = true;
        audioManager.GetComponent<AudioScript>().musicSource.Play();
        while (enemies.Count > 0 && duration > 0)
        {
            if(timerText == null){
                yield break;
            }
            timerText.text = floatToMinutesSeconds(duration);
            yield return null;
            duration -= Time.deltaTime;

            for (int i = 0; i < enemies.Count; i++)
            {

                if(enemies[i] != null){
                    switch(enemies[i].tag){
                        case "Soldier":
                            if(enemies[i].GetComponent<SoldierScript>().isKilled()){
                                int xp = enemies[i].GetComponent<SoldierScript>().getXP();
                                player.GetComponent<PlayerScript>().addXP(xp);
                                player.GetComponent<PlayerScript>().addKill();
                                Destroy(enemies[i]);
                                enemies.RemoveAt(i);
                            }

                            break;
                        case "Drone":
                            if(enemies[i].GetComponent<DroneScript>().isKilled()){
                                int xp = enemies[i].GetComponent<DroneScript>().getXP();
                                player.GetComponent<PlayerScript>().addXP(xp);
                                player.GetComponent<PlayerScript>().addKill();
                                Destroy(enemies[i]);
                                enemies.RemoveAt(i);
                            }
                            break;

                        case "Boss":
                            if(enemies[i].GetComponent<BossScript>().isKilled()){
                                int xp = enemies[i].GetComponent<BossScript>().getXP();
                                player.GetComponent<PlayerScript>().addXP(xp);
                                player.GetComponent<PlayerScript>().addKill();
                                Destroy(enemies[i]);
                                enemies.RemoveAt(i);
                            }
                            break;
                    }
                }
            }
        }
        //game over
        if (duration <= 0)
        {
            SceneManager.LoadScene("GameOverScene");
        }
    }

    // Update UI
    public void updateUI(PlayerStats playerStats){
        if (healthBar == null || xpBar == null || levelText == null || waveText == null || timerText == null || ammoText == null || magazineText == null || weaponNameText == null || medkitsCountText == null)
        {
            return;
        }
        healthText.text = playerStats.health.ToString();
        healthBar.value = playerStats.health;
        healthBar.maxValue = playerStats.maxHealth;
        medkitsCountText.text = playerStats.medkits.ToString();

        int currentLevel = 0;
        int xpCount = 0;
        int previousLevel = levelText.text.StartsWith("LEVEL ") 
        ? int.Parse(levelText.text.Substring(6)) - 1 
        : 0;

        while(currentLevel < levels.Count - 1 && xpCount + levels[currentLevel].xpToNextLevel <= playerStats.xp) // Calculate current level
        {
            xpCount += levels[currentLevel].xpToNextLevel;
            currentLevel++;
        }

        if(previousLevel != currentLevel){
            audioManager.GetComponent<AudioScript>().playLevelUp();
        }

        // Update xp bar and level text
        int difference = playerStats.xp - xpCount;
        int xpForNextLevel = levels[currentLevel].xpToNextLevel;
        float differencePercentage = (float)difference / xpForNextLevel;
        xpBar.value = differencePercentage;
        levelText.text = "LEVEL " + ++currentLevel;
        ammoText.text = playerStats.ammo.ToString();
        magazineText.text = playerStats.magazine.ToString();
        weaponNameText.text = playerStats.weaponName;
    }
    
   void Awake()
    {
        seed = Math.Abs(Guid.NewGuid().GetHashCode()); // Calculate random seed
        random = new System.Random(seed); // Set random seed
        DontDestroyOnLoad(gameObject);
        difficultyManager = GameObject.FindGameObjectWithTag("DifficultyManager");

        if (difficultyManager != null)
        {
            currentDifficulty = difficultyManager.GetComponent<DifficultyManager>().getDifficulty();
            Debug.Log("Current Difficulty: " + currentDifficulty);
        }
        else
        {
            Debug.LogError("DifficultyManager not found in the scene.");
        }

        levels.Add(new Level(100));
        levels.Add(new Level(200));
        levels.Add(new Level(400));
        levels.Add(new Level(600));
        levels.Add(new Level(950));
        levels.Add(new Level(1300));
        levels.Add(new Level(1800));
        levels.Add(new Level(2500));
        levels.Add(new Level(3400));
        levels.Add(new Level(4500));
        levels.Add(new Level(5800));
        levels.Add(new Level(7400));
        levels.Add(new Level(9300));
        levels.Add(new Level(11500));
        levels.Add(new Level(14000));
        levels.Add(new Level(17000));
        levels.Add(new Level(20500));
        levels.Add(new Level(24500));
        levels.Add(new Level(29000));
        levels.Add(new Level(34000));

        
        weaponThresholds = new Dictionary<int, GameObject> //<xp, weapon>
            {
                { 200, smgPrefab },
                { 600, shotgunPrefab },
                { 1200, laserPrefab },
                { 3000, plasmaCannonPrefab }
            };

        spawnedThresholds = new HashSet<int>();

        // Initialize empty block grid for pathfinding
        blockGrid = new int[width * 2, height];
            for (int x = 0; x < blockGrid.GetLength(0); x++)
            {
                for (int y = 0; y < blockGrid.GetLength(1); y++)
                {
                    blockGrid[x, y] = 1; 
                }
            }

        // Initialize range weapons
        weapons.Add("Basic Pistol", new RangeWeapon(damage: 20, maxAmmo: 8, ammo: 8, magazine: 72, name: "Basic Pistol", offsetX: 0.55f, offsetY: 0.1f, bulletSpeed: 10f, bulletLifeTime: 3f, shotDelay: 0.25f, bulletPrefab: baseBulletPrefab, weaponIcon: basePistolIcon));
        weapons.Add("Smg", new RangeWeapon(damage: 10, maxAmmo: 10, ammo: 10, magazine: 40, name: "Smg", offsetX: 1.1f, offsetY: 0.15f, bulletSpeed: 20f, bulletLifeTime: 2f, shotDelay: 0.1f, bulletPrefab: baseBulletPrefab, weaponIcon: smgIcon));
        weapons.Add("Shotgun", new RangeWeapon(damage: 20, maxAmmo: 7, ammo: 7, magazine: 21, name: "Shotgun", offsetX: 0.9f, offsetY: 0f, bulletSpeed: 10f, bulletLifeTime: 4f, shotDelay: 0.5f, bulletPrefab: baseBulletPrefab, weaponIcon: shotgunIcon));
        weapons.Add("Laser Gun", new RangeWeapon(damage: 35, maxAmmo: 5, ammo: 5, magazine: 30, name: "Laser Gun", offsetX: 0.9f, offsetY: 0f, bulletSpeed: 200f, bulletLifeTime: 5f, shotDelay: 0.5f, bulletPrefab: invisBulletPrefab, weaponIcon: laserIcon));
        weapons.Add("Plasma Cannon", new RangeWeapon(damage: 50, maxAmmo: 5, ammo: 5, magazine: 20, name: "Plasma Cannon", offsetX: 0.9f, offsetY: 0f, bulletSpeed: 20f, bulletLifeTime: 7f, shotDelay: 0.7f, bulletPrefab: plasmaBulletPrefab, weaponIcon: plasmaCannonIcon));

        audioManager = GameObject.FindGameObjectWithTag("AudioManager");

        Debug.Log(seed);
    }


    // Function to get the next block for the drone and boss to move to
    public (int, int) getNextBlock(float srcX, float srcY)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        int enemyX = (int)Math.Round(srcX) + width;
        int enemyY = (int)Math.Round(srcY);
        int playerX = (int)Math.Round(player.transform.position.x) + width;
        int playerY = (int)Math.Round(player.transform.position.y) + 1;
        
        return AStarSearch.getNextMove(enemyX, enemyY, playerX, playerY);
    }

    // Start is called before the first frame update
    void Start()
    {
      generate();
      StartCoroutine(handleWaves());
      AStarSearch = new AStar(blockGrid);
    }
    

    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerScript>().getPlayerStats();
            updateUI(playerStats);
            totalElapsedTime += Time.deltaTime;

            if (player.GetComponent<PlayerScript>().isKilled()) // If the player is killed, go to the game over scene
            {
                //player = null;
                Destroy(player);
                SceneManager.LoadScene("GameOverScene");
            
            }

            if(weaponThresholds == null || spawnedThresholds == null){
                return;
            }
            foreach (int threshold in weaponThresholds.Keys) // Spawn weapons if player reaches the required xp
            {
                if (playerStats.xp >= threshold && !spawnedThresholds.Contains(threshold))
                {
                    Vector3 spawnPoint = generateRandomSpawnPoint(offsetY: 1);
                    Instantiate(weaponThresholds[threshold], spawnPoint, Quaternion.identity);
                    spawnedThresholds.Add(threshold);
                }
            }
        }
    }
}
