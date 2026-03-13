import pygame
from random import randint

pygame.init()
SCREEN_WIDTH, SCREEN_HEIGHT = 800, 600
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
pygame.display.set_caption("Simple Racer")
clock = pygame.time.Clock()

class RacingCar:
    WIDTH, HEIGHT = 50, 100
    WHEEL_WIDTH = 10
    WHEEL_HEIGHT = 20
    WHEEL_OFFSET = 10
    WHEEL_DISTANCE = 60

    def __init__(self, x: float, y: float, is_player: bool) -> None:
        self.x = x
        self.y = y
        self.is_player = is_player
        self.color = "blue" if is_player else "red"
        self.speed = 150 if is_player else 225

    def draw(self) -> None:
        pygame.draw.rect(screen, self.color,
                         (self.x, self.y, RacingCar.WIDTH, RacingCar.HEIGHT))
        pygame.draw.rect(screen, "black",
                         (self.x - RacingCar.WHEEL_WIDTH // 2,
                          self.y + RacingCar.WHEEL_OFFSET,
                          RacingCar.WHEEL_WIDTH, RacingCar.WHEEL_HEIGHT))
        pygame.draw.rect(screen, "black",
                         (self.x - RacingCar.WHEEL_WIDTH // 2,
                          self.y + RacingCar.WHEEL_OFFSET + RacingCar.WHEEL_DISTANCE,
                          RacingCar.WHEEL_WIDTH, RacingCar.WHEEL_HEIGHT))
        pygame.draw.rect(screen, "black",
                         (self.x + RacingCar.WIDTH - RacingCar.WHEEL_WIDTH // 2,
                          self.y + RacingCar.WHEEL_OFFSET,
                          RacingCar.WHEEL_WIDTH, RacingCar.WHEEL_HEIGHT))
        pygame.draw.rect(screen, "black",
                         (self.x + RacingCar.WIDTH - RacingCar.WHEEL_WIDTH // 2,
                          self.y + RacingCar.WHEEL_OFFSET + RacingCar.WHEEL_DISTANCE,
                          RacingCar.WHEEL_WIDTH, RacingCar.WHEEL_HEIGHT))

    def move(self, dt: float) -> None:
        keys = pygame.key.get_pressed()
        if self.is_player:
            if keys[pygame.K_LEFT]:
                self.x -= self.speed * dt
            if keys[pygame.K_RIGHT]:
                self.x += self.speed * dt
            if keys[pygame.K_UP]:
                self.y -= self.speed * dt
            if keys[pygame.K_DOWN]:
                self.y += self.speed * dt
            
            # Keep player within screen bounds
            self.x = max(0, min(SCREEN_WIDTH - RacingCar.WIDTH, self.x))
            self.y = max(0, min(SCREEN_HEIGHT - RacingCar.HEIGHT, self.y))
        else:
            # Enemy cars move downward
            self.y += self.speed * dt


# Game state variables
running = True
game_over = False
score = 0
enemy_spawn_timer = 0
enemy_spawn_interval = 0.75  # seconds

player_car = RacingCar(SCREEN_WIDTH // 2 - 25, SCREEN_HEIGHT - 150, True)
enemy_cars: list[RacingCar] = []
game_font = pygame.font.Font(None, 30)


def reset_game() -> None:
    global game_over, score, enemy_cars, player_car
    game_over = False
    score = 0
    enemy_cars.clear()
    player_car = RacingCar(SCREEN_WIDTH // 2 - 25, SCREEN_HEIGHT - 150, True)


def check_collision(car1: RacingCar, car2: RacingCar) -> bool:
    return (car1.x < car2.x + RacingCar.WIDTH and
            car1.x + RacingCar.WIDTH > car2.x and
            car1.y < car2.y + RacingCar.HEIGHT and
            car1.y + RacingCar.HEIGHT > car2.y)


while running:
    dt = clock.tick(60) / 1000

    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            running = False
        if event.type == pygame.KEYDOWN:
            if event.key == pygame.K_r and game_over:
                reset_game()

    if not game_over:
        # Spawn enemy cars with random x position
        enemy_spawn_timer += dt
        if enemy_spawn_timer >= enemy_spawn_interval:
            enemy_spawn_timer = 0
            enemy_x = randint(0, SCREEN_WIDTH - RacingCar.WIDTH)
            enemy_cars.append(RacingCar(enemy_x, -RacingCar.HEIGHT, False))

        # Update game objects
        player_car.move(dt)
        
        # Move enemy cars and check for collisions
        for enemy_car in enemy_cars[:]:
            enemy_car.move(dt)
            
            # Check collision with player
            if check_collision(player_car, enemy_car):
                game_over = True
            
            # Remove enemy cars that have gone off screen
            if enemy_car.y > SCREEN_HEIGHT:
                enemy_cars.remove(enemy_car)
                if not game_over:
                    score += 1

    # Drawing
    screen.fill("lightblue")
    
    # Draw road
    pygame.draw.rect(screen, "darkgray", (0, 0, SCREEN_WIDTH, SCREEN_HEIGHT))
    
    # Draw lane markings
    for i in range(0, SCREEN_HEIGHT, 40):
        pygame.draw.rect(screen, "white", (SCREEN_WIDTH // 2 - 5, i, 10, 20))
    
    # Draw cars
    player_car.draw()
    for enemy_car in enemy_cars:
        enemy_car.draw()
    
    # Draw UI
    score_text = game_font.render(f"Score: {score}", True, "white")
    screen.blit(score_text, (10, 10))
    
    if game_over:
        pygame.draw.rect(screen, "black",
                         (SCREEN_WIDTH // 2 - 155, SCREEN_HEIGHT // 2 - 10, 325, 40))
        game_over_text = game_font.render("GAME OVER! Press 'R' to restart", True, "red")
        screen.blit(game_over_text, (SCREEN_WIDTH // 2 - 150, SCREEN_HEIGHT // 2))

    pygame.display.flip()

pygame.quit()