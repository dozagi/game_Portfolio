// Fill out your copyright notice in the Description page of Project Settings.

#include "RightHandMotionController.h"
#include "Public/MotionControllerComponent.h" 
#include "Components/SkeletalMeshComponent.h" 
#include "Components/SphereComponent.h" 
#include "SteamVRChaperoneComponent.h"

#include "UObject/ConstructorHelpers.h"

#include "Animation/AnimBlueprint.h" 

#include "Equipment/PlayerSword.h"
#include "Components/StaticMeshComponent.h" 
#include "Engine/StreamableManager.h"
#include "Item/Potion.h"					
#include "Kismet/GameplayStatics.h"							
#include "MyCharacter/MotionControllerCharacter.h"
#include "Item/PotionBag.h"
#include "GameInstance/VRGameInstance.h"

#include "Components/BoxComponent.h"
#include "Monster/Dog/Dog.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Components/CapsuleComponent.h"

#include "Kismet/KismetMathLibrary.h"

#include "Components/WidgetInteractionComponent.h"

#include "Monster/Dog/Dog.h"
#include "Monster/Dog/DogAIController.h"
#include "Object/Door/LockKey.h"		
#include "Object/Door/DoorLock.h"		
#include "Object/Door/LockedDoor.h"	

// 검을 가진 오른손 코드입니다
// 공격 이외에도 포션을 잡거나 개 몬스터가 물면 흔들어 떨어뜨릴 수 있습니다.

// Sets default values
ARightHandMotionController::ARightHandMotionController()
{
	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	// 루트를 생성합니다
	MotionController = CreateDefaultSubobject<UMotionControllerComponent>(TEXT("MotionController"));
	SetRootComponent(MotionController);

	// 손 형태의 메쉬를 생성, 루트에 붙입니다
	HandMesh = CreateDefaultSubobject<USkeletalMeshComponent>(TEXT("HandMesh"));
	HandMesh->SetupAttachment(MotionController);

	// 에디터 상에서 메쉬를 찾아 적용합니다
	static ConstructorHelpers::FObjectFinder<USkeletalMesh>SK_RightHand(TEXT("SkeletalMesh'/Game/Assets/CharacterEquipment/MyCharacter/Hand/Mesh/MannequinHand_Right.MannequinHand_Right'"));
	if (SK_RightHand.Succeeded()) 
	{
		HandMesh->SetSkeletalMesh(SK_RightHand.Object);
	}

	HandMesh->SetRelativeLocation(FVector(-15.0f, 1.9f, 9.9f));
	HandMesh->SetRelativeRotation(FRotator(-45.0f, 0, 90.0f)); 
	HandMesh->bGenerateOverlapEvents = true;		
	HandMesh->SetCollisionProfileName(FName("NoCollision"));	

	// 손 주변의 물건을 감지하기 위해 콜리전을 생성합니다
	OverlapSphere = CreateDefaultSubobject<USphereComponent>(TEXT("OverlapSphere"));
	OverlapSphere->SetupAttachment(HandMesh);

	OverlapSphere->SetCollisionProfileName("OverlapAll");
	OverlapSphere->SetRelativeLocation(FVector(9.0f, 3.4f, -1.6f));
	OverlapSphere->SetSphereRadius(7.0f);
	OverlapSphere->bHiddenInGame = true;

	// UI와 상호 작용할 수 있는 컴포넌트를 생성합니다.
	interaction = CreateDefaultSubobject<UWidgetInteractionComponent>(TEXT("Interaction"));
	interaction->SetupAttachment(OverlapSphere);

	interaction->TraceChannel = ECollisionChannel::ECC_Visibility;
	interaction->InteractionDistance = 100.0f;

	// 상호 작용 영역을 표시하기는 컴포넌트를 생성합니다. 
	SteamVRChaperone = CreateDefaultSubobject<USteamVRChaperoneComponent>(TEXT("SteamVRChaperone"));

	// 열쇠를 잡을 위치의 컴포넌트를 생성합니다 
	KeySocket = CreateDefaultSubobject<USceneComponent>(TEXT("KeySocket"));
	KeySocket->SetupAttachment(HandMesh);

	KeySocket->SetRelativeLocation(FVector(10.0f, 0.0f, 0.5f));
	KeySocket->SetRelativeRotation(FRotator(88.0f, 180.0f, 180.0f));

	// 포션을 잡을 위치의 컴포넌트를 생성합니다.
	PotionPosition = CreateDefaultSubobject<USceneComponent>(TEXT("PotionPosition"));
	PotionPosition->SetupAttachment(HandMesh);

	PotionPosition->SetRelativeLocation(FVector(11.768803f, 0.847741f, -3.021931f));
	PotionPosition->SetRelativeRotation(FRotator(3.364257f, -86.961861f, 177.434341f));

	// 개가 물 위치의 컴포넌트를 생성합니다.
	AttachDogPosition = CreateDefaultSubobject<USceneComponent>(TEXT("AttachDogPosition"));
	AttachDogPosition->SetupAttachment(HandMesh);

	AttachDogPosition->SetRelativeLocation(FVector(-10.0f, 71.845093f, -38.933586f));
	AttachDogPosition->SetRelativeRotation(FRotator(0.0f, -90.0f, 0.0f));

	// 검을 잡을 위치의 컴포넌트를 생성합니다.
	SwordAttachScene = CreateDefaultSubobject<USceneComponent>(TEXT("WeaponScene"));
	SwordAttachScene->SetupAttachment(HandMesh);

	SwordAttachScene->SetRelativeRotation(FRotator(0, 60.0f, -175.082443f)); 
	SwordAttachScene->SetRelativeLocation(FVector(3.238229f, 5.621831f, -3.814407f)); 

	// 다른 팀원이 작성한 코드입니다.
	// 모션 컨트롤러 Motion Source에 넣기 위해 열거형을 이름으로 형 변환하여 넣습니다
	FString HandName = GetEnumToString(Hand);
	MotionController->MotionSource = (FName(*HandName)); 

	// 손에 사용할 애니메이션 블루프린트를 찾아 적용합니다. 
	static ConstructorHelpers::FObjectFinder<UClass>ABP_Hand(TEXT("AnimBlueprint'/Game/Blueprints/MyCharacter/Hand/ABP_RightHand.ABP_RightHand_C'"));

	if (ABP_Hand.Succeeded()) 
	{
		UClass* RightHandAnimBlueprint = ABP_Hand.Object;

		if (RightHandAnimBlueprint)
		{
			HandMesh->SetAnimationMode(EAnimationMode::AnimationBlueprint);  //손의 애니메이션 모드를 블루프린트 형으로 바꾸고
			HandMesh->SetAnimInstanceClass(RightHandAnimBlueprint);  //위에서 찾은 애니메이션 블루프린트를 넣습니다
		}
	}
	// 여기까지 입니다.
	
	// 초기값 설정
	WantToGrip = true;
	AttachedActor = nullptr;
	HandState = E_HandState::Grab;
	HandFormState = EHandFormState::WeaponHandGrab;
	Hand = EControllerHand::Right;
	VisibleSwordFlag = true; 
	HandTouchActorFlag = true;
	bisRightGrab = false;
	bGrabPotion = false;	

	Tags.Add(FName(TEXT("RightHand"))); 
	Tags.Add(FName(TEXT("DisregardForLeftHand")));
	Tags.Add(FName(TEXT("DisregardForRightHand")));
}

// Called when the game starts or when spawned
void ARightHandMotionController::BeginPlay()
{
	Super::BeginPlay();

	HandOwner = Cast<AMotionControllerCharacter>(UGameplayStatics::GetPlayerCharacter(GetWorld(), 0));

	FActorSpawnParameters SpawnActorOption;		// 액터를 생성하기위한 구조체
	SpawnActorOption.Owner = this;		// 생성할 액터의 주인을 현재 액터로 합니다.

	//	액터를 생성할 때, 충돌처리를 어떻게 할지 정합니다.
	SpawnActorOption.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;	  // 충돌에 관계없이 생성합니다

	FAttachmentTransformRules AttachRules(EAttachmentRule::SnapToTarget, EAttachmentRule::SnapToTarget, EAttachmentRule::KeepWorld, false);

	// 현재 월드에 검을 생성합니다.
	Sword = GetWorld()->SpawnActor<APlayerSword>(Sword->StaticClass(), SwordAttachScene->GetComponentLocation(), SwordAttachScene->GetComponentRotation(), SpawnActorOption);

	if (Sword)
	{
		Sword->AttachToComponent(HandMesh, AttachRules, TEXT("CharacterSwordSocket"));
	}

	// 현재 손의 오버랩 이벤트 
	OverlapSphere->OnComponentBeginOverlap.AddDynamic(this, &ARightHandMotionController::OnHandBeginOverlap);
	OverlapSphere->OnComponentEndOverlap.AddDynamic(this, &ARightHandMotionController::OnHandEndOverlap);
}

// Called every frame
void ARightHandMotionController::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	AActor* NearestMesh;	// 손 주변에 있는 액터를 저장할 변수

	// 잡고 있는 상태, 그랩상태, 검이 있는 상태일 경우
	if (AttachedActor != nullptr || WantToGrip == true || VisibleSwordFlag)
	{
		HandState = E_HandState::Grab;	// Grab
	}
	else
	{
		NearestMesh = GetActorNearHand();		// 손 주변의 액터를 확인
		if (NearestMesh != nullptr)		// 액터가 존재할 때 
		{
			HandState = E_HandState::CanGrab;		// 잡고 있는 상태로 바꿉니다.
		}
		else									// 액터가 존재하고 있지 않을 때
		{
			if (WantToGrip)			// 액터가 없는 데도 그랩상태이면
			{
				HandState = E_HandState::Grab;		// Grab상태로 변환
			}
			else						// 액터도 없고 그랩 상태도 없으면 
			{
				HandState = E_HandState::Open;		 // 오픈 상태로 바꿉니다.
			}
		}
	}

	// 개가 팔을 물고 있을 때, 속도에 따라 스택이 변화됩니다. 일정 스택이 쌓이면 개는 떨어집니다. 
	if (AttachDog)
	{
		Prelinear = Currentlinear;

		HandCurrentPosistion = OverlapSphere->GetComponentLocation() - GetActorLocation();
		HandMoveDelta = HandCurrentPosistion - HandPreviousPosistion;
		HandMoveVelocity = HandMoveDelta / DeltaTime;
		HandPreviousPosistion = HandCurrentPosistion;

		Currentlinear = HandMoveVelocity.Size();

		if (Currentlinear > 45.0f) stack++;
		else
		{
			if (stack > 0) stack = 0;
		}
	}
}

// Grab버튼을 눌렀을 때, 호출되는 함수
void ARightHandMotionController::GrabActor()
{
	if (bGrabKey) return;
	AActor* NearestMesh;		// 가까이 있는 액터를 저장해 두는 변수

	WantToGrip = true;			
	bisRightGrab = true;

	if (HandTouchActorFlag)			// 손에 액터가 충돌해 있는 상태일 경우
	{
		HandFormState = EHandFormState::PotionHandGrab;
		NearestMesh = GetActorNearHand(); 
		if (NearestMesh)			// 가까지 있는 액터가 존재할 경우
		{
			Sword->SetActorHiddenInGame(true);	// 검을 숨깁니다

			// 문
			if (NearestMesh->ActorHasTag("Door"))
			{
				AttachedActor = NearestMesh;
				// 문은 손에 붙여지지 않습니다
			}

			// 다른 팀원이 작성한 코드입니다
			// 포션
			else if (NearestMesh->ActorHasTag("PotionBag"))
			{
				APotionBag* PotionBag = Cast<APotionBag>(NearestMesh);

				// 포션가방에서 그랩하면 남아있는 포션이 있을 때 손에 생성됩니다.
				if (PotionBag)
				{
					if (PotionBag->Potions.Num()>0)
					{
						// 충돌 처리 여부를 결정하고 월드에 생성합니다.
						FActorSpawnParameters SpawnActorOption; 
						SpawnActorOption.Owner = this; 
						SpawnActorOption.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
						FAttachmentTransformRules AttachRules(EAttachmentRule::SnapToTarget, EAttachmentRule::SnapToTarget, EAttachmentRule::KeepWorld, false);
						Potion = PotionBag->PotionPop();

						// 포션을 생성하고 손에 부착
						if (Potion)
						{
							AttachedActor = Potion;
							if (!Potion->Mesh->IsPendingKill())
							{
								Potion->Mesh->SetCollisionProfileName(TEXT("OverlapAll"));
								Potion->SetActorRelativeScale3D(FVector(1.1f, 1.1f, 1.1f));
								Potion->AttachToComponent(PotionPosition, AttachRules);
								HandGrabState();
								bGrabPotion = true;
							}							
						}

						// 게임 인스턴스에서 현재 포션의 수를 갱신합니다.
						UVRGameInstance* GI = Cast<UVRGameInstance>(UGameplayStatics::GetGameInstance(GetWorld()));

						if (GI)
						{
							GI->PotionCountUpdate(PotionBag->PotionCount);
						}
					}
					else
					{
						// 남아있는 포션이 없을 경우는 생성되지 않습니다.
						HandNomalState();
					}
				}
			}
			// 여기 까지 입니다.
			else
			{
				// 손에 부착 됩니다.
				AttachedActor = NearestMesh;
				FAttachmentTransformRules AttachRules(EAttachmentRule::KeepWorld, EAttachmentRule::KeepWorld, EAttachmentRule::KeepWorld, false);
				NearestMesh->GetRootComponent()->AttachToComponent(MotionController, AttachRules);
			}
		}
	}
}

// Grab해제
void ARightHandMotionController::ReleaseActor()
{
	if (bGrabKey) return;

	bisRightGrab = false;
	if (AttachedActor)
	{
		HandFormState = EHandFormState::WeaponHandGrab;
		WantToGrip = false;
		VisibleSwordFlag = false;	

		// 잡은 것을 뗍니다.
		if (AttachedActor->GetRootComponent()->GetAttachParent() == MotionController)
		{
			// 문은 제외됩니다.
			if (!AttachedActor->ActorHasTag("Door"))
			{
				AttachedActor->DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);
			}
		}
		// 포션을 잡고 있을 때
		else if (AttachedActor->GetRootComponent()->GetAttachParent() == PotionPosition)
		{
			if (AttachedActor->ActorHasTag("Potion"))
			{
				APotion* AttachPotion = Cast<APotion>(AttachedActor);
				if (AttachPotion)
				{
					AActor* NearestMesh = GetActorNearHand();

					if (NearestMesh)
					{
						if (NearestMesh->ActorHasTag("PotionBag"))
						{
							// 포션 가방의 범위에서 그랩을 해제하면 포션이 넣어지게 됩니다.
							APotionBag* PotionBag = Cast<APotionBag>(NearestMesh);
							if (PotionBag)
							{
								PotionBag->PotionPush(AttachPotion);
								HandNomalState();
							}
							// 이외의 곳에는 떨어져 사라집니다
							else
							{
								bGrabPotion = false;
								AttachPotion->BagInputFlag = true;
								AttachPotion->Mesh->SetCollisionProfileName("NoCollision");
								AttachPotion->Mesh->SetSimulatePhysics(true);
								AttachedActor->DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);		
							}
						}
					}
					else
					{
						AttachPotion->BagInputFlag = true;
						AttachPotion->Mesh->SetSimulatePhysics(true);
						AttachedActor->DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);
					}
				}
			}
		}
		AttachedActor = nullptr;	
	}
	else			// 메뉴의 범위에 있을 경우
	{
		if (Sword->bHidden)		
			HandOpenState();		// 오픈 상태로 변환
	}
}

// 가까운 액터 획득
AActor * ARightHandMotionController::GetActorNearHand()
{
	TArray<AActor*> OverlappingActors;

	FVector GrabSphereLocation;
	FVector OverlappingActorLocation;
	FVector SubActorLocation;
	AActor* NearestOverlappingActor = nullptr;
	float NearestOverlap = 10000.0f;

	OverlapSphere->GetOverlappingActors(OverlappingActors, AActor::StaticClass());			// 오버랩된 액터를 저장합니다.
	GrabSphereLocation = OverlapSphere->GetComponentLocation();

	for (AActor* OverlappingActor : OverlappingActors)
	{
		if (OverlappingActor->ActorHasTag("DisregardForRightHand") || OverlappingActor->ActorHasTag("DisregardForLeftHand"))
		{
			continue;
		}
		else if (OverlappingActor->ActorHasTag("PotionBag"))
		{
			NearestOverlappingActor = OverlappingActor;
			break;
		}
		else if (OverlappingActor->ActorHasTag("Door"))
		{
			NearestOverlappingActor = OverlappingActor;
			break;
		}
		else
		{
			OverlappingActorLocation = OverlappingActor->GetActorLocation();
			SubActorLocation = OverlappingActorLocation - GrabSphereLocation;
			if (SubActorLocation.Size() < NearestOverlap)
			{
				NearestOverlappingActor = OverlappingActor;
				break;
			}
		}
	}
	return NearestOverlappingActor;
}

// 검을 쥐고 있는 보통 상태
void ARightHandMotionController::HandNomalState()
{
	HandTouchActorFlag = false;
	WantToGrip = true;
	VisibleSwordFlag = true;
	Sword->SetActorHiddenInGame(false); 
	AttachedActor = nullptr;		
}

// 검이 보이지 않는 손이 오픈되어 있는 상태 
void ARightHandMotionController::HandOpenState()
{
	HandTouchActorFlag = true;
	WantToGrip = false;
	VisibleSwordFlag = false;
	Sword->SetActorHiddenInGame(true); 
}

// 검이 보이지 않고 쥐고 있는 상태 
void ARightHandMotionController::HandGrabState()
{
	HandTouchActorFlag = false;
	WantToGrip = true;
	VisibleSwordFlag = false;
	Sword->SetActorHiddenInGame(true);
}

// 오버랩 이벤트
void ARightHandMotionController::OnHandBeginOverlap(UPrimitiveComponent * OverlappedComponent, AActor * OtherActor, UPrimitiveComponent * OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult & SweepResult)
{
	if (OtherActor->ActorHasTag("LockedDoor"))
	{
		if (HandOwner->bHasKey)
			LockedDoor = Cast<ALockedDoor>(OtherActor);
	}


	if (OtherComp->ComponentHasTag("BodyLock"))
	{
		if (HandOwner->bHasKey)
		{
			LockedDoor->GetLockKey();
			HandNomalState();
			Key->Destroy();
			LockedDoor = NULL;
		}
		return;
	}

	if (bGrabKey) return;

	// 열쇠를 소지하고 있는 상태에서 자물쇠에 범위에 있으면 열쇠가 생성됩니다.
	if (OtherComp->ComponentHasTag("LockArea"))
	{
		if (HandOwner->bHasKey)
		{
			bGrabKey = true;
			HandGrabState();
			FActorSpawnParameters SpawnActorOption; 
			SpawnActorOption.Owner = this; 
			SpawnActorOption.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
			FAttachmentTransformRules AttachRules(EAttachmentRule::SnapToTarget, EAttachmentRule::SnapToTarget, EAttachmentRule::KeepWorld, false);

			Key = GetWorld()->SpawnActor<ALockKey>(Key->StaticClass(), KeySocket->GetComponentLocation(), KeySocket->GetComponentRotation(), SpawnActorOption);

			if (Key)
			{
				Key->AttachToComponent(KeySocket, AttachRules);
			}
		}
		return;
	}

	// 잡을 수 없는 물체 : 포션가방, 머리, 문 등 
	if (OtherComp->ComponentHasTag("DisregardForLeftHand") || OtherComp->ComponentHasTag("DisregardForRightHand"))
	{
		if (OtherComp->ComponentHasTag("Head"))
		{			
			HandNomalState();			
		}
		return;
	}
	if (AttachedActor)
	{
		return;
	}
	// 메뉴의 공간 범위내에 들어가면 검은 보이지 않고 오픈되어있는 상태가 됩니다. 
	if (OtherComp->ComponentHasTag("GrabRange"))
	{
		HandOpenState();
		return;
	}

	if (OtherActor->ActorHasTag("PotionBag"))		
	{
		HandOpenState();
		return;
	}
	else if (OtherActor->ActorHasTag("Door"))	
	{
		HandOpenState();
		return;
	}
	else if (OtherActor->ActorHasTag("DisregardForRightHand"))
	{
		return;
	}
	else 	
	{
		HandOpenState();
		return;
	}
}

// 오버랩 해제 이벤트
void ARightHandMotionController::OnHandEndOverlap(UPrimitiveComponent * OverlappedComponent, AActor * OtherActor, UPrimitiveComponent * OtherComp, int32 OtherBodyIndex)
{
	if (OtherActor->ActorHasTag("LockedDoor"))
		LockedDoor = NULL;

	// 자물쇠 범위를 빠져나가면 열쇠도 사라집니다
	if (OtherComp->ComponentHasTag("LockArea"))
	{
		if (Key)
		{
			bGrabKey = false;
			Key->Destroy();
		}
		HandNomalState();
	}

	if (OtherComp->ComponentHasTag("DisregardForLeftHand") || OtherComp->ComponentHasTag("DisregardForRightHand"))
		return;

	// 메뉴 공간 범위를 빠져나가면 원래 상태가 됩니다. 
	if (OtherComp->ComponentHasTag("GrabRange"))
	{
		HandNomalState();
		return;
	}

	if (OtherActor->ActorHasTag("DisregardForRightHand"))
		return;

	if (HandState != E_HandState::Grab)
		HandNomalState();

	return;
}

// 다른 팀원이 작성한 코드입니다 
FString ARightHandMotionController::GetEnumToString(EControllerHand Value)
{
	UEnum* enumPtr = FindObject<UEnum>(ANY_PACKAGE, TEXT("EControllerHand"), true);
	if (!enumPtr)
	{
		return FString("Invalid");
	}
	return enumPtr->GetEnumName((int32)Value);
}
// 여기까지 입니다.