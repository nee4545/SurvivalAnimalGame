Animal Unlock UI System

How to use:
1. Copy the AnimalUnlockUISystem folder into your Unity project's Assets folder.
2. Let Unity compile.
3. In Unity, go to Tools > Wild Paws UI > Create Animal Unlock UI Prefab.
4. Unity will create: Assets/AnimalUnlockUISystem/Generated/AnimalUnlockPanel.prefab
5. Drag AnimalUnlockPanel.prefab into your Canvas.
6. On AnimalUnlockPanel, assign your player object with CCActor.
7. Create Animal Unlock Data assets from: Create > Wild Paws > Animal Unlock Data.
8. Add those data assets to the Animals list on AnimalCollectionUI.
9. Replace the sample cards by using the runtime list flow. You can delete SampleAnimalCard objects from Content after confirming scrolling works.

Important setup details included:
- ScrollRect vertical only
- Viewport with transparent Image + RectMask2D
- Content top anchored
- GridLayoutGroup with Fixed Column Count = 3
- GridScrollContentFitter to force correct content height
- Decorative images/text have Raycast Target off
