﻿#include "Dog.h"

#include "UObject/ConstructorHelpers.h"
#include "Components/CapsuleComponent.h"
#include "Components/BoxComponent.h"

#include "GameFramework/CharacterMovementComponent.h"
#include "Perception/PawnSensingComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Animation/AnimSequence.h"

#include "Kismet/GameplayStatics.h"
#include "MyCharacter/MotionControllerCharacter.h"

#include "Monster/Dog/DogAIController.h"
#include "BehaviorTree/BehaviorTree.h"
#include "BehaviorTree/BlackboardComponent.h"
#include "BehaviorTree/BehaviorTreeComponent.h"

#include "Animation/AnimMontage.h"
#include "Animation/AnimBlueprint.h"

#include "Camera/CameraComponent.h"

#include "Kismet/KismetMathLibrary.h"

#include "HandMotionController/RightHandMotionController.h"
#include "HandMotionController/LeftHandMotionController.h"
#include "Components/SphereComponent.h"

#include "Equipment/PlayerSword.h"			
#include "TimerManager.h"		
#include "Engine/World.h"

// 플레이어를 감지하고 주위를 돌아 팔을 무는 몬스터입니다.

ADog::ADog()
{
	// Set this character to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	// 메쉬를 찾아서 적용합니다.
	static ConstructorHelpers::FObjectFinder<USkeletalMesh>MonsterMesh(TEXT("SkeletalMesh'/Game/Assets/CharacterEquipment/Monster/Dog/Mesh2/MON_DOG_MESH.MON_DOG_MESH'"));
	if (MonsterMesh.Succeeded())
	{
		GetMesh()->SetSkeletalMesh(MonsterMesh.Object);
	}

	// 비헤이비어 트리를 찾아 적용합니다
	static ConstructorHelpers::FObjectFinder<UBehaviorTree>Monster_BehaviorTree(TEXT("BehaviorTree'/Game/Blueprints/Monster/Dog/AI/RagdollDogBT_2.RagdollDogBT_2'"));
	if (Monster_BehaviorTree.Succeeded())
	{
		BehaviorTree = Monster_BehaviorTree.Object;
	}

	// 애니메이션을 찾아 적용합니다.
	// 기존에 사용 하던 UAnimBlueprint는 패키징 이후에 엔진이 찾을수 없는것으로 판단되어 새롭게 UClass로 오브젝트를 찾아 넣는 형식으로 바꿨습니다.
	static ConstructorHelpers::FObjectFinder<UClass>Monster_AnimBlueprint(TEXT("AnimBlueprint'/Game/Blueprints/Monster/Dog/Blueprints2/ABP_Dog_3.ABP_Dog_3_C'"));

	if (Monster_AnimBlueprint.Succeeded())
	{
		UClass* DogAnimBlueprint = Monster_AnimBlueprint.Object;

		if (DogAnimBlueprint)
		{
			GetMesh()->SetAnimationMode(EAnimationMode::AnimationBlueprint);
			GetMesh()->SetAnimInstanceClass(DogAnimBlueprint);
		}
	}

	static ConstructorHelpers::FObjectFinder<UMaterialInterface>Monster_Material(TEXT("MaterialInstanceConstant'/Game/Assets/CharacterEquipment/Monster/Dog/Materials/NewFolder/M_Dog_noElite_Inst.M_Dog_noElite_Inst'"));
	if (Monster_Material.Succeeded())
	{
		NomalMaterial = Monster_Material.Object;
	}

	static ConstructorHelpers::FObjectFinder<UMaterialInterface>Monster_Material_Elite(TEXT("MaterialInstanceConstant'/Game/Assets/CharacterEquipment/Monster/Dog/Materials/NewFolder/M_Dog_Elite_Inst.M_Dog_Elite_Inst'"));
	if (Monster_Material_Elite.Succeeded())
	{
		EliteMaterial = Monster_Material_Elite.Object;
	}

	// 개의 머리의 콜리전을 생성합니다.
	DogAttackCollision = CreateDefaultSubobject<UBoxComponent>(TEXT("DogAttack"));
	DogAttackCollision->SetupAttachment(GetMesh(), TEXT("HeadSocket"));

	// 감각 컴포넌트를 생성합니다.
	PawnSensing = CreateDefaultSubobject<UPawnSensingComponent>(TEXT("PawnSensing"));

	// AI컨트롤러를 적용합니다
	AIControllerClass = ADogAIController::StaticClass();

	ADog::GetCapsuleComponent()->SetCapsuleHalfHeight(55.0f);
	ADog::GetCapsuleComponent()->SetCapsuleRadius(30.0f);
	GetMesh()->SetRelativeLocation(FVector(-20.0f, 0.0f, -55.0f));
	GetMesh()->SetRelativeRotation(FRotator(0.0f, -90.0f, 0.0f));

	GetCapsuleComponent()->SetCollisionProfileName("IgnoreOnlyPawn");

	DogAttackCollision->SetCollisionProfileName(TEXT("OverlapAll"));
	DogAttackCollision->SetRelativeLocation(FVector(10.0f, -10.0f, 0.0f));
	DogAttackCollision->SetRelativeScale3D(FVector(0.6f, 0.6f, 0.6f));
	DogAttackCollision->SetActive(false);
	DogAttackCollision->ComponentTags.Add("DogAttackCollision");
	DogAttackCollision->bGenerateOverlapEvents = false;

	// 감각 정보 설정
	PawnSensing->bHearNoises = true;
	PawnSensing->HearingThreshold = 1200.0f;
	PawnSensing->LOSHearingThreshold = 1400.0f;
	PawnSensing->bSeePawns = true;
	PawnSensing->SetPeripheralVisionAngle(40.0f);
	PawnSensing->SightRadius = 1200.0f;
	PawnSensing->SensingInterval = 0.1f;

	bOnLand = true;			// 땅에 있는지
	Landing = false;		// 착지인지아닌지

	MaxHP = 1.0f;
	CurrentHP = MaxHP;
	bIsDeath = false;
	bIsDetach = false;

	AttackWaite = false;

	BiteDamage = 10.0f;

	GetMesh()->SetSimulatePhysics(false);
	GetMesh()->SetCollisionProfileName("Ragdoll");
	GetCharacterMovement()->MaxWalkSpeed = 0.0f;
	Tags.Add("Monster");
	Tags.Add("Dog");
	Tags.Add(FName(TEXT("DisregardForLeftHand")));
	Tags.Add(FName(TEXT("DisregardForRightHand")));
}

// Called when the game starts or when spawned
void ADog::BeginPlay()
{
	Super::BeginPlay();

	if (bIsElite)
		GetMesh()->SetMaterial(0, EliteMaterial);
	else
		GetMesh()->SetMaterial(0, NomalMaterial);

	// 상태 초기화
	CurrentDogState = EDogState::Idle;
	CurrentDogAnimState = EDogAnimState::Idle;
	CurrentDogBattleState = EDogBattleState::Nothing;
	CurrentDogAirState = EDogAirState::Nothing;
	CurrentDogJumpState = EDogJumpState::Nothing;
	CurrentDogCircleState = EDogCircleState::Nothing;

	// 개의 머리와 오버랩될 때, 실시하는 함수를 등록
	DogAttackCollision->OnComponentBeginOverlap.AddDynamic(this, &ADog::OnAttackCollisionOverlap);

	if (PawnSensing)
	{
		PawnSensing->OnSeePawn.AddDynamic(this, &ADog::OnSeePlayer);
		PawnSensing->OnHearNoise.AddDynamic(this, &ADog::OnHearNoise);
	}
}

// Called every frame
void ADog::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	AI = Cast<ADogAIController>(GetController());
	FFindFloorResult FloorDistance;
	GetCharacterMovement()->ComputeFloorDist(GetCapsuleComponent()->GetComponentLocation(), 10000.0f, 10000.0f, FloorDistance, 34.0f);

	// 블랙 보드에 정보를 실시간으로 보냅니다.
	if (AI)
	{
		AI->BBComponent->SetValueAsEnum("CurrentDogState", (uint8)CurrentDogState);
		AI->BBComponent->SetValueAsEnum("CurrentDogAnimState", (uint8)CurrentDogAnimState);
		AI->BBComponent->SetValueAsEnum("CurrentDogJumpState", (uint8)CurrentDogJumpState);
		AI->BBComponent->SetValueAsEnum("CurrentDogCircleState", (uint8)CurrentDogCircleState);
		AI->BBComponent->SetValueAsEnum("CurrentDogBattleState", (uint8)CurrentDogBattleState);
		AI->BBComponent->SetValueAsEnum("CurrentDogAirState", (uint8)CurrentDogAirState);
		AI->BBComponent->SetValueAsObject("AttachActor", AttachActor);
		AI->BBComponent->SetValueAsBool("bOnLand", bOnLand);
		AI->BBComponent->SetValueAsBool("DeathFlag", bIsDeath);
		AI->BBComponent->SetValueAsFloat("HP", CurrentHP);
		height = FloorDistance.FloorDist;
		CurrentFalling = GetCharacterMovement()->IsFalling();
	}

}

// Called to bind functionality to input
void ADog::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

}

// 플레이어 감지
void ADog::OnSeePlayer(APawn * Pawn)
{
	FFindFloorResult FloorDistance;;
	// 땅과의 거리를 확인합니다.
	GetCharacterMovement()->ComputeFloorDist(GetCapsuleComponent()->GetComponentLocation(), 10000.0f, 10000.0f, FloorDistance, 34, 0);

	// 감지한 액터의 태그가 Character일 때, 그 액터는 개의 타겟이 됩니다. 
	if (Pawn->ActorHasTag("Character") && FloorDistance.FloorDist < 3.0f)
	{
		ADogAIController* AI = Cast<ADogAIController>(GetController());

		if (AI && !AI->BBComponent->GetValueAsObject("Player"))
		{
			// 상태 설정
			Target = Pawn;
			CurrentDogState = EDogState::Chase;
			CurrentDogAnimState = EDogAnimState::Run;
			CurrentDogJumpState = EDogJumpState::Nothing;
			CurrentDogCircleState = EDogCircleState::Nothing;
			AI->BBComponent->SetValueAsObject("Player", Pawn);
			GetCharacterMovement()->MaxWalkSpeed = 550.0f;
		}
	}
}

void ADog::OnHearNoise(APawn * Pawn, const FVector & Location, float Volume)
{
	if (!Target)
	{
		if (Pawn->ActorHasTag(TEXT("Character")))
		{
			AMotionControllerCharacter* Mycharacter = Cast<AMotionControllerCharacter>(Pawn);

			if (Mycharacter)
			{
				Target = Mycharacter;
				CurrentDogState = EDogState::Chase;
				CurrentDogAnimState = EDogAnimState::Run;
				CurrentDogJumpState = EDogJumpState::Nothing;
				CurrentDogCircleState = EDogCircleState::Nothing;
				AI->BBComponent->SetValueAsObject("Player", Pawn);
				GetCharacterMovement()->MaxWalkSpeed = 550.0f;
			}
		}
	}
}

void ADog::BiteAttack()
{
	if (AttachActor)
	{
		UGameplayStatics::ApplyDamage(UGameplayStatics::GetPlayerCharacter(GetWorld(), 0), BiteDamage, GetController(), this, nullptr);		// 오버랩된 액터에 데미지 전달
		GetWorld()->GetTimerManager().SetTimer(AttackHandle, this, &ADog::BiteAttack, 0.1f, false);
	}
	else
		GetWorld()->GetTimerManager().ClearTimer(AttackHandle);			// 스테미너 사용 동작은 잠시 스테미너 회복을 멈춤
}

// 오버랩함수. 개가 플레이어의 머리와 오버랩되면 팔을 물게하는 함수입니다.
void ADog::OnAttackCollisionOverlap(UPrimitiveComponent * OverlappedComp, AActor * OtherActor, UPrimitiveComponent * OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult & SweepResult)
{
	if (OtherComp->ComponentHasTag("Head"))
	{
		AMotionControllerCharacter* Character = Cast<AMotionControllerCharacter>(UGameplayStatics::GetPlayerCharacter(GetWorld(), 0));

		if (AttachActor == NULL && Character)
		{
			ARightHandMotionController* RightController = Cast<ARightHandMotionController>(Character->RightHand);

			if (!RightController->AttachDog)
			{
				RightController->AttachDog = this;

				DogAttackCollision->bGenerateOverlapEvents = false;	

				// 붙입니다
				FAttachmentTransformRules AttachRules(EAttachmentRule::SnapToTarget, EAttachmentRule::SnapToTarget, EAttachmentRule::KeepWorld, true);

				AttachToComponent(RightController->AttachDogPosition, AttachRules);

				// 개를 레그돌화 시킵니다
				GetMesh()->SetAllBodiesBelowSimulatePhysics("Bip002-Spine1", true, true);			// Neck이하는 모조리 피직스 부여

				SetActorRelativeLocation(FVector(0.0f, 0.0f, 0.0f));
				SetActorRelativeRotation(FRotator(0.0f, 0.0f, 0.0f));

				AttachActor = RightController;
				AI->BBComponent->SetValueAsBool("bIsBiting", true);

				BiteAttack();
			}
		}
	}
}

// 데미지를 받는 함수입니다.
float ADog::TakeDamage(float Damage, FDamageEvent const & DamageEvent, AController * EventInstigator, AActor * DamageCauser)
{
	APlayerSword* Sword = Cast<APlayerSword>(DamageCauser);
	if (CurrentDogState == EDogState::Bite && Sword)
	{
		return 0;
	}
	CurrentHP -= Damage;

	// 주먹으로 맞거나 검으로 피격당하면 쓰러집니다
	if (CurrentHP < 0.0f)
	{
		DogAttackCollision->bGenerateOverlapEvents = false;

		if (CurrentDogState == EDogState::Bite)
		{
			CurrentDogAnimState = EDogAnimState::FallingDeath;
			bIsDeath = true;
			bpunchDetach = true;
		}
		else if (CurrentDogState == EDogState::Battle)
		{
			CurrentDogState = EDogState::Death;
		}
	}

	return Damage;
}

