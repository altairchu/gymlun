export const bodyFieldGroups = [
  [
    '主要指標',
    [
      ['weightKg', '體重', 'kg'],
      ['bmi', 'BMI', ''],
      ['bodyFatPercent', '體脂率', '%'],
      ['heartRateBpm', '心率', 'bpm'],
      ['bodyScore', '體組成評分', '分'],
    ],
  ],
  [
    '體組成',
    [
      ['muscleMassKg', '肌肉量', 'kg'],
      ['musclePercent', '肌肉率', '%'],
      ['proteinMassKg', '蛋白質量', 'kg'],
      ['proteinPercent', '蛋白質率', '%'],
      ['bodyWaterKg', '水分量', 'kg'],
      ['bodyWaterPercent', '水分率', '%'],
      ['boneMineralKg', '骨礦物質量', 'kg'],
      ['boneMineralPercent', '骨礦物質率', '%'],
      ['fatMassKg', '脂肪量', 'kg'],
      ['fatFreeMassKg', '去脂體重', 'kg'],
      ['skeletalMuscleKg', '骨骼肌量', 'kg'],
    ],
  ],
  [
    '健康評估',
    [
      ['visceralFatLevel', '內臟脂肪等級', ''],
      ['waistHipRatio', '腰臀比', ''],
      ['skeletalMuscleIndex', '骨骼肌指數', 'kg/m²'],
      ['basalMetabolicRateKcal', '報告基礎代謝', 'kcal'],
      ['bodyAgeYears', '身體年齡', '歲'],
      ['suggestedIntakeKcal', '報告建議攝取', 'kcal'],
    ],
  ],
] as const;

export const bodyProfileFields = [
  ['ageYears', '年齡', '歲'],
  ['heightCm', '身高', 'cm'],
  ['waistCm', '腰圍', 'cm'],
  ['hipCm', '臀圍', 'cm'],
] as const;

export const bodyRegions = [
  ['leftArm', '左臂'],
  ['rightArm', '右臂'],
  ['trunk', '軀幹'],
  ['leftLeg', '左腿'],
  ['rightLeg', '右腿'],
] as const;

export const segmentFields = [
  ['fatMassKg', '脂肪量'],
  ['muscleMassKg', '肌肉量'],
] as const;
